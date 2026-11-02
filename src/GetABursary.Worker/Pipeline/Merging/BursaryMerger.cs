using System.Text.Json;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Reports;
using GetABursary.Core.Repositories.Bursaries;
using GetABursary.Core.Repositories.Reports;
using GetABursary.Worker.Extraction;
using GetABursary.Worker.Sources;

namespace GetABursary.Worker.Pipeline.Merging;

/// <summary>Merges one parsed and extracted listing into exactly one canonical bursary (spec 02 §4).</summary>
public sealed class BursaryMerger(
    IBursaryWriteRepository bursaries,
    IFunderRepository funders,
    IReviewItemRepository reviewItems,
    IClock clock) : IBursaryMerger
{
    public const string EligibilityLock = "Eligibility";

    private const double CandidateThreshold = 0.4;
    private const double AutoMergeSimilarity = 0.6;
    private const double NoFunderDuplicateSimilarity = 0.8;
    private const int MaxClosingDateGapDays = 45;
    private const decimal LowConfidenceThreshold = 0.7m;

    public async Task<MergeOutcome> MergeAsync(
        Guid sourceId,
        Uri sourceUrl,
        Guid rawListingId,
        RawListing raw,
        ExtractedEligibility ex,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sourceUrl);
        ArgumentNullException.ThrowIfNull(raw);
        ArgumentNullException.ThrowIfNull(ex);

        var now = clock.UtcNow;
        var rawListing = await bursaries.GetRawListingAsync(rawListingId, ct);
        var funder = await GetOrCreateFunderAsync(ex.FunderName ?? raw.FunderName, ct);
        var normalizedName = NameNormalizer.Normalize(raw.Title);

        // The same listing seen again always updates its own bursary.
        var existingId = rawListing?.BursaryId;
        Guid? mergeTargetId = existingId;
        BursaryCandidate? possibleDuplicate = null;

        if (mergeTargetId is null)
        {
            var closing = raw.ClosingDate ?? ex.ClosingDate;
            var candidates = normalizedName.Length == 0
                ? []
                : await bursaries.FindCandidatesAsync(normalizedName, CandidateThreshold, ct);
            (mergeTargetId, possibleDuplicate) = Decide(candidates, funder?.Id, closing);
        }

        MergeOutcome outcome;
        Bursary bursary;
        if (mergeTargetId is { } targetId)
        {
            bursary = await bursaries.GetForMergeAsync(targetId, ct)
                ?? throw new InvalidOperationException($"Bursary {targetId} disappeared during merge.");
            var unchangedText = rawListing is not null && rawListing.LastChangedAt < rawListing.LastFetchedAt;
            await ApplyAsync(bursary, raw, ex, funder, unchangedText, now, ct);
            UpsertSource(bursary, sourceId, sourceUrl, now);
            bursary.LastSeenAt = now;
            outcome = new MergeOutcome(MergeKind.Merged, bursary.Id);

            if (bursary.ExtractionConfidence < LowConfidenceThreshold
                && !await reviewItems.HasOpenAsync(bursary.Id, ReviewItemType.LowConfidence, ct))
            {
                reviewItems.Add(NewReviewItem(
                    ReviewItemType.LowConfidence, bursary.Id, rawListingId, new { confidence = bursary.ExtractionConfidence }));
            }
        }
        else
        {
            bursary = await CreateAsync(raw, ex, funder, normalizedName, ct);
            await ApplyAsync(bursary, raw, ex, funder, unchangedText: false, now, ct);
            UpsertSource(bursary, sourceId, sourceUrl, now);
            bursary.LastSeenAt = now;
            bursaries.Add(bursary);

            if (possibleDuplicate is not null)
            {
                outcome = new MergeOutcome(MergeKind.PossibleDuplicate, bursary.Id);
                reviewItems.Add(NewReviewItem(
                    ReviewItemType.PossibleDuplicate,
                    bursary.Id,
                    rawListingId,
                    new { candidateBursaryId = possibleDuplicate.Id, similarity = possibleDuplicate.Similarity }));
            }
            else
            {
                outcome = new MergeOutcome(MergeKind.Created, bursary.Id);
                reviewItems.Add(NewReviewItem(
                    ReviewItemType.NewListing, bursary.Id, rawListingId, new { confidence = bursary.ExtractionConfidence }));
            }
        }

        if (rawListing is not null)
        {
            rawListing.BursaryId = bursary.Id;
        }

        await bursaries.SaveChangesAsync(ct);
        return outcome;
    }

    /// <summary>Applies spec §4.3–4.5 to the candidates (best similarity first): merge target, or a possible duplicate, or neither.</summary>
    private static (Guid? MergeInto, BursaryCandidate? PossibleDuplicate) Decide(
        IReadOnlyList<BursaryCandidate> candidates, Guid? funderId, DateOnly? closing)
    {
        var merge = candidates.FirstOrDefault(c =>
            funderId is not null
            && c.FunderId == funderId
            && c.Similarity >= AutoMergeSimilarity
            && DatesCompatible(c.ClosingDate, closing));
        if (merge is not null)
        {
            return (merge.Id, null);
        }

        var duplicate = candidates.FirstOrDefault(c =>
            funderId is not null
                ? c.FunderId == funderId && c.Similarity is >= CandidateThreshold and < AutoMergeSimilarity
                : c.FunderId is null && c.Similarity >= NoFunderDuplicateSimilarity);
        return (null, duplicate);
    }

    private static bool DatesCompatible(DateOnly? a, DateOnly? b) =>
        a is null || b is null || Math.Abs(a.Value.DayNumber - b.Value.DayNumber) <= MaxClosingDateGapDays;

    private async Task<Funder?> GetOrCreateFunderAsync(string? funderName, CancellationToken ct)
    {
        var normalized = NameNormalizer.Normalize(funderName);
        if (normalized.Length == 0)
        {
            return null;
        }

        var funder = await funders.GetByNormalizedNameAsync(normalized, ct);
        if (funder is null)
        {
            funder = new Funder { Name = Truncate(funderName!.Trim(), 200)!, NormalizedName = normalized };
            funders.Add(funder);
        }

        return funder;
    }

    private async Task<Bursary> CreateAsync(
        RawListing raw, ExtractedEligibility ex, Funder? funder, string normalizedName, CancellationToken ct)
    {
        var name = Truncate(raw.Title.Trim(), 300)!;
        var id = Guid.CreateVersion7();
        var slug = SlugGenerator.Create(name, id);

        // The first 6 hex chars of a v7 GUID are a coarse timestamp (changes every ~4.6h), so equal names created
        // close together collide. Fall back to extra random characters from the id.
        if (await bursaries.SlugExistsAsync(slug, ct))
        {
            slug = $"{slug}-{id.ToString("N")[^6..]}";
        }

        return new Bursary
        {
            Id = id,
            Slug = slug,
            Name = name,
            NormalizedName = normalizedName,
            FunderId = funder?.Id,
            Funder = funder,
            IsPublished = false,
        };
    }

    private async Task ApplyAsync(
        Bursary b, RawListing raw, ExtractedEligibility ex, Funder? funder, bool unchangedText, DateTimeOffset now, CancellationToken ct)
    {
        if (!b.IsLocked(nameof(Bursary.Name)))
        {
            b.Name = Truncate(raw.Title.Trim(), 300)!;
            b.NormalizedName = NameNormalizer.Normalize(raw.Title);
        }

        if (funder is not null && !b.IsLocked(nameof(Bursary.FunderId)))
        {
            b.Funder = funder;
            b.FunderId = funder.Id;
        }

        Set(b, nameof(Bursary.Summary), () => b.Summary = Truncate(ex.Summary, 600));
        Set(b, nameof(Bursary.ValueSummary), () => b.ValueSummary = Truncate(raw.ValueText, 500) ?? b.ValueSummary);
        Set(b, nameof(Bursary.Coverage), () => b.Coverage = [.. ex.Coverage]);
        Set(b, nameof(Bursary.WorkBackRequired), () => b.WorkBackRequired = ex.WorkBackRequired ?? b.WorkBackRequired);
        Set(b, nameof(Bursary.WorkBackDetails), () => b.WorkBackDetails = Truncate(ex.WorkBackDetails, 1000) ?? b.WorkBackDetails);
        Set(b, nameof(Bursary.StudyLevels), () => b.StudyLevels = [.. ex.StudyLevels]);
        Set(b, nameof(Bursary.MinAverage), () => b.MinAverage = ex.MinAverage);
        Set(b, nameof(Bursary.MinSubjectRequirements), () => b.MinSubjectRequirements = [.. ex.SubjectRequirements]);
        Set(b, nameof(Bursary.Citizenship), () => b.Citizenship = ex.Citizenship);
        Set(b, nameof(Bursary.Provinces), () => b.Provinces = [.. ex.Provinces]);
        Set(b, nameof(Bursary.AllFieldsEligible), () => b.AllFieldsEligible = ex.AllFieldsEligible);
        Set(b, nameof(Bursary.OpenDate), () => b.OpenDate = raw.OpenDate ?? ex.OpenDate ?? b.OpenDate);
        Set(b, nameof(Bursary.ClosingDate), () => b.ClosingDate = raw.ClosingDate ?? ex.ClosingDate ?? b.ClosingDate);
        Set(b, nameof(Bursary.ApplyUrl), () =>
        {
            var url = raw.ApplyUrl?.ToString();
            b.ApplyUrl = url is { Length: <= 1000 } ? url : b.ApplyUrl;
        });

        var confidence = Math.Round(Math.Clamp(ex.Confidence, 0m, 1m), 2);
        b.ExtractionConfidence = unchangedText ? Math.Max(b.ExtractionConfidence, confidence) : confidence;

        var today = clock.Today;
        b.Status = b.ClosingDate is not { } closing ? BursaryStatus.Unknown
            : closing < today ? BursaryStatus.Closed
            : BursaryStatus.Open;

        if (!b.IsLocked(EligibilityLock))
        {
            await ReplaceLinksAsync(b, ex, ct);
        }
    }

    private static void Set(Bursary b, string fieldName, Action assign)
    {
        if (!b.IsLocked(fieldName))
        {
            assign();
        }
    }

    private async Task ReplaceLinksAsync(Bursary b, ExtractedEligibility ex, CancellationToken ct)
    {
        var fieldIds = await bursaries.GetFieldIdsByCodesAsync(ex.FieldCodes, ct);
        var qualificationIds = await bursaries.GetQualificationIdsAsync(ex.QualificationNames, ex.InstitutionShortNames, ct);
        var institutionIds = await bursaries.GetInstitutionIdsByShortNamesAsync(ex.InstitutionShortNames, ct);

        Sync(b.Fields, fieldIds, x => x.FieldOfStudyId, id => new BursaryField { BursaryId = b.Id, FieldOfStudyId = id });
        Sync(b.Qualifications, qualificationIds, x => x.QualificationId, id => new BursaryQualification { BursaryId = b.Id, QualificationId = id });
        Sync(b.Institutions, institutionIds, x => x.InstitutionId, id => new BursaryInstitution { BursaryId = b.Id, InstitutionId = id });
    }

    /// <summary>Diffs a join collection so unchanged links are left alone (no delete-and-reinsert of the same key).</summary>
    private static void Sync<T>(ICollection<T> current, IReadOnlyList<Guid> target, Func<T, Guid> key, Func<Guid, T> create)
    {
        var wanted = target.ToHashSet();
        foreach (var stale in current.Where(x => !wanted.Contains(key(x))).ToList())
        {
            current.Remove(stale);
        }

        var have = current.Select(key).ToHashSet();
        foreach (var id in wanted.Where(id => !have.Contains(id)))
        {
            current.Add(create(id));
        }
    }

    private static void UpsertSource(Bursary b, Guid sourceId, Uri sourceUrl, DateTimeOffset now)
    {
        var url = sourceUrl.ToString();
        var link = b.Sources.FirstOrDefault(s => s.SourceId == sourceId && s.SourceUrl == url);
        if (link is null)
        {
            b.Sources.Add(new BursarySource
            {
                BursaryId = b.Id,
                SourceId = sourceId,
                SourceUrl = url,
                FirstSeenAt = now,
                LastSeenAt = now,
            });
        }
        else
        {
            link.LastSeenAt = now;
        }
    }

    private static ReviewItem NewReviewItem(ReviewItemType type, Guid bursaryId, Guid rawListingId, object details) =>
        new()
        {
            Type = type,
            BursaryId = bursaryId,
            RawListingId = rawListingId,
            Details = JsonSerializer.SerializeToDocument(details),
        };

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}
