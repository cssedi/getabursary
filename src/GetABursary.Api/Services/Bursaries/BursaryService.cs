using GetABursary.Api.DTOs.Bursaries;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Repositories.Bursaries;

namespace GetABursary.Api.Services.Bursaries;

public sealed class BursaryService(IBursaryReadRepository bursaries, IClock clock) : IBursaryService
{
    public async Task<Result<BursaryDetailOutcome>> GetDetailAsync(string slug, CancellationToken ct)
    {
        var bursary = await bursaries.GetBySlugAsync(slug, ct);
        if (bursary is null || !bursary.IsPublished)
        {
            return BursaryErrors.NotFound(slug);
        }

        var summary = ToSummary(bursary, clock.Today);
        if (bursary.Status == BursaryStatus.Closed)
        {
            return new BursaryDetailOutcome(Detail: null, ClosedSummary: summary);
        }

        return new BursaryDetailOutcome(ToDetail(bursary, summary), ClosedSummary: null);
    }

    private static BursarySummaryResponse ToSummary(Bursary bursary, DateOnly today) => new()
    {
        Id = bursary.Id,
        Slug = bursary.Slug,
        Name = bursary.Name,
        FunderName = bursary.Funder?.Name,
        ClosingDate = bursary.ClosingDate,
        IsClosingSoon = bursary.IsClosingSoon(today),
        Status = bursary.Status.ToString(),
        StudyLevels = [.. bursary.StudyLevels.Select(l => l.ToString())],
        MinAverage = bursary.MinAverage,
        Coverage = [.. bursary.Coverage.Select(c => c.ToString())],
        Sources = [.. bursary.Sources
            .OrderBy(s => s.Source.Name)
            .ThenBy(s => s.SourceUrl, StringComparer.Ordinal)
            .Select(s => new BursarySourceResponse
            {
                Key = s.Source.Key,
                Name = s.Source.Name,
                IconUrl = s.Source.IconUrl,
                Url = s.SourceUrl,
            })],
        // shortcut: a detail lookup is not ranked against a search, so matchScore is always 0. Drop the field from the summary if a non-search caller needs a real value.
        MatchScore = 0m,
    };

    private static BursaryDetailResponse ToDetail(Bursary bursary, BursarySummaryResponse summary) => new()
    {
        Id = summary.Id,
        Slug = summary.Slug,
        Name = summary.Name,
        FunderName = summary.FunderName,
        ClosingDate = summary.ClosingDate,
        IsClosingSoon = summary.IsClosingSoon,
        Status = summary.Status,
        StudyLevels = summary.StudyLevels,
        MinAverage = summary.MinAverage,
        Coverage = summary.Coverage,
        Sources = summary.Sources,
        MatchScore = summary.MatchScore,
        Summary = bursary.Summary,
        ValueSummary = bursary.ValueSummary,
        WorkBackRequired = bursary.WorkBackRequired,
        WorkBackDetails = bursary.WorkBackDetails,
        MinSubjectRequirements = [.. bursary.MinSubjectRequirements.Select(r => new SubjectRequirementResponse(r.Subject, r.MinPercent))],
        Citizenship = bursary.Citizenship.ToString(),
        Provinces = [.. bursary.Provinces.Select(p => p.ToString())],
        Fields = [.. bursary.Fields
            .OrderBy(f => f.FieldOfStudy.Code, StringComparer.Ordinal)
            .Select(f => new BursaryFieldResponse(f.FieldOfStudy.Code, f.FieldOfStudy.Name))],
        Qualifications = [.. bursary.Qualifications
            .OrderBy(q => q.Qualification.Name, StringComparer.Ordinal)
            .Select(q => new BursaryQualificationResponse(
                q.Qualification.Id,
                q.Qualification.Name,
                q.Qualification.Faculty.Institution.ShortName))],
        Institutions = [.. bursary.Institutions
            .OrderBy(i => i.Institution.ShortName, StringComparer.Ordinal)
            .Select(i => new BursaryInstitutionResponse(i.Institution.Id, i.Institution.ShortName))],
        OpenDate = bursary.OpenDate,
        // The funder's own apply page wins; otherwise the listing we saw most recently is the best link we have.
        ApplyUrl = bursary.ApplyUrl ?? bursary.Sources.MaxBy(s => s.LastSeenAt)?.SourceUrl,
        UpdatedAt = bursary.UpdatedAt,
    };
}

public static class BursaryErrors
{
    public static Error NotFound(string slug) =>
        Error.NotFound("Bursary.NotFound", $"Bursary {slug} was not found");

    public static Error Closed(string slug) =>
        Error.Gone("Bursary.Closed", $"Bursary {slug} is closed");
}
