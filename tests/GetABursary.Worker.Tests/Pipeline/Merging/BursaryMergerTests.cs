using GetABursary.Core.Common;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;
using GetABursary.Core.Domain.Taxonomy;
using GetABursary.Core.Repositories.Bursaries;
using GetABursary.Core.Repositories.Reports;
using GetABursary.Worker.Extraction;
using GetABursary.Worker.Pipeline.Merging;
using GetABursary.Worker.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SourceListing = GetABursary.Worker.Sources.RawListing;

namespace GetABursary.Worker.Tests.Pipeline.Merging;

[Collection("Postgres")]
public class BursaryMergerTests(PostgresFixture fixture)
{
    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }

    private static readonly DateTimeOffset T0 = new(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly FakeClock _clock = new(T0);

    private async Task<Guid> CreateSourceAsync()
    {
        await using var db = fixture.CreateContext();
        var source = new Source { Key = "src-" + Guid.NewGuid().ToString("N"), Name = "Merge Source", BaseUrl = "https://merge.test" };
        db.Sources.Add(source);
        await db.SaveChangesAsync();
        return source.Id;
    }

    private static ExtractedEligibility Extracted(
        string? funder,
        DateOnly? closing,
        decimal confidence = 0.9m,
        IReadOnlyList<string>? qualifications = null,
        IReadOnlyList<string>? institutions = null,
        decimal? minAverage = 65m) =>
        new([], false, qualifications ?? [], institutions ?? [], [StudyLevel.Undergraduate], minAverage, [],
            Citizenship.SouthAfrican, [], [CoverageItem.Tuition], null, null, closing, null, funder, "Summary", confidence);

    private async Task<(MergeOutcome Outcome, Guid RawListingId)> MergeAsync(
        Guid sourceId,
        string title,
        string? funder,
        DateOnly? closing,
        decimal confidence = 0.9m,
        string? url = null,
        Guid? rawListingId = null,
        IReadOnlyList<string>? qualifications = null,
        IReadOnlyList<string>? institutions = null,
        decimal? minAverage = 65m)
    {
        url ??= $"https://merge.test/{Guid.NewGuid():N}";
        await using var db = fixture.CreateContext(_clock);

        if (rawListingId is null)
        {
            var row = new RawListing
            {
                SourceId = sourceId,
                Url = url,
                ContentHash = new string('a', 64),
                Html = "<html></html>",
                FirstFetchedAt = T0,
                LastFetchedAt = T0,
                LastChangedAt = T0,
            };
            db.RawListings.Add(row);
            await db.SaveChangesAsync();
            rawListingId = row.Id;
        }

        var raw = new SourceListing(new Uri(url), title, funder, closing, null, null, "Eligibility text", null, null);
        var merger = new BursaryMerger(
            new BursaryWriteRepository(db), new FunderRepository(db), new ReviewItemRepository(db), _clock);
        var outcome = await merger.MergeAsync(
            sourceId, new Uri(url), rawListingId.Value, raw, Extracted(funder, closing, confidence, qualifications, institutions, minAverage), CancellationToken.None);
        return (outcome, rawListingId.Value);
    }

    private async Task AssertSimilarityAsync(string a, string b, double min, double maxExclusive)
    {
        await using var db = fixture.CreateContext();
        await using var conn = new NpgsqlConnection(db.Database.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("select similarity(@a, @b)", conn);
        cmd.Parameters.AddWithValue("a", NameNormalizer.Normalize(a));
        cmd.Parameters.AddWithValue("b", NameNormalizer.Normalize(b));
        var sim = (float)(await cmd.ExecuteScalarAsync())!;
        Assert.InRange(sim, min, maxExclusive - 0.0001);
    }

    [Fact]
    public async Task MergeAsync_NewBursary_CreatedUnpublishedWithReviewItemAndSource()
    {
        var sourceId = await CreateSourceAsync();
        var (outcome, rawId) = await MergeAsync(sourceId, "Mbombela Chemistry Award 2026", "Zeta Holdings", new DateOnly(2026, 9, 30));

        Assert.Equal(MergeKind.Created, outcome.Kind);
        await using var db = fixture.CreateContext();
        var bursary = await db.Bursaries.Include(b => b.Sources).Include(b => b.Funder).SingleAsync(b => b.Id == outcome.BursaryId);
        Assert.False(bursary.IsPublished);
        Assert.Equal(BursaryStatus.Open, bursary.Status);
        Assert.Equal("mbombela chemistry award", bursary.NormalizedName);
        Assert.StartsWith("mbombela-chemistry-award-2026-", bursary.Slug, StringComparison.Ordinal);
        Assert.Equal("zeta holdings", bursary.Funder!.NormalizedName);
        Assert.Equal(T0, bursary.LastSeenAt);
        var link = Assert.Single(bursary.Sources);
        Assert.Equal(T0, link.FirstSeenAt);
        Assert.Equal(rawId, (await db.RawListings.SingleAsync(r => r.Id == rawId)).Id);
        Assert.Equal(outcome.BursaryId, (await db.RawListings.SingleAsync(r => r.Id == rawId)).BursaryId);
        var item = await db.ReviewItems.SingleAsync(r => r.BursaryId == outcome.BursaryId);
        Assert.Equal(ReviewItemType.NewListing, item.Type);
        Assert.Equal(ReviewStatus.Open, item.Status);
    }

    [Fact]
    public async Task MergeAsync_ClosedAndUndatedListings_GetClosedAndUnknownStatus()
    {
        var sourceId = await CreateSourceAsync();
        var closed = await MergeAsync(sourceId, "Ntabankulu Welding Prize", "Eta Group", new DateOnly(2026, 5, 31));
        var undated = await MergeAsync(sourceId, "Vukuzenzele Plumbing Grant", "Theta Group", null);

        await using var db = fixture.CreateContext();
        Assert.Equal(BursaryStatus.Closed, (await db.Bursaries.SingleAsync(b => b.Id == closed.Outcome.BursaryId)).Status);
        Assert.Equal(BursaryStatus.Unknown, (await db.Bursaries.SingleAsync(b => b.Id == undated.Outcome.BursaryId)).Status);
    }

    [Fact]
    public async Task MergeAsync_SameFunderSimilarNameCloseDates_MergesWithTwoSources()
    {
        var first = await CreateSourceAsync();
        var second = await CreateSourceAsync();
        var created = await MergeAsync(first, "Khanyisa Mining Excellence Bursary 2026", "Khanyisa Trust", new DateOnly(2026, 9, 30));
        await AssertSimilarityAsync("Khanyisa Mining Excellence", "Khanyisa Mining Excellence Fund", 0.6, 1.01);

        _clock.UtcNow = T0.AddDays(2);
        var merged = await MergeAsync(second, "Khanyisa Mining Excellence Fund", "KHANYISA TRUST", new DateOnly(2026, 10, 15), minAverage: 70m);

        Assert.Equal(MergeKind.Merged, merged.Outcome.Kind);
        Assert.Equal(created.Outcome.BursaryId, merged.Outcome.BursaryId);
        await using var db = fixture.CreateContext();
        var bursary = await db.Bursaries.Include(b => b.Sources).SingleAsync(b => b.Id == created.Outcome.BursaryId);
        Assert.Equal(2, bursary.Sources.Count);
        Assert.Equal(new DateOnly(2026, 10, 15), bursary.ClosingDate);
        Assert.Equal(70m, bursary.MinAverage);
        Assert.Equal(T0.AddDays(2), bursary.LastSeenAt);
        Assert.Equal(1, await db.Funders.CountAsync(f => f.NormalizedName == "khanyisa trust"));
        Assert.Equal(1, await db.Bursaries.CountAsync(b => b.NormalizedName.StartsWith("khanyisa mining")));
        Assert.Equal(created.Outcome.BursaryId, (await db.RawListings.SingleAsync(r => r.Id == merged.RawListingId)).BursaryId);
    }

    [Fact]
    public async Task MergeAsync_SameFunderSimilarNameFarDates_CreatesNewBursary()
    {
        var sourceId = await CreateSourceAsync();
        var first = await MergeAsync(sourceId, "Sipho Dairy Science Award", "Iota Foundation", new DateOnly(2026, 7, 1));
        var second = await MergeAsync(sourceId, "Sipho Dairy Science Award", "Iota Foundation", new DateOnly(2026, 11, 1));

        Assert.Equal(MergeKind.Created, second.Outcome.Kind);
        Assert.NotEqual(first.Outcome.BursaryId, second.Outcome.BursaryId);
    }

    [Fact]
    public async Task MergeAsync_SameNameDifferentFunder_CreatesNewBursary()
    {
        var sourceId = await CreateSourceAsync();
        var first = await MergeAsync(sourceId, "Lwazi Textile Design Prize", "Kappa Corp", new DateOnly(2026, 8, 1));
        var second = await MergeAsync(sourceId, "Lwazi Textile Design Prize", "Lambda Corp", new DateOnly(2026, 8, 1));

        Assert.Equal(MergeKind.Created, second.Outcome.Kind);
        Assert.NotEqual(first.Outcome.BursaryId, second.Outcome.BursaryId);
    }

    [Fact]
    public async Task MergeAsync_SameFunderSimilarityBetween04And06_PossibleDuplicateNotMerged()
    {
        var sourceId = await CreateSourceAsync();
        var first = await MergeAsync(sourceId, "Thandolwethu Nursing Care", "Mu Trust", new DateOnly(2026, 9, 1));
        const string other = "Thandolwethu Nursing Fund Trust Africa";
        await AssertSimilarityAsync("Thandolwethu Nursing Care", other, 0.4, 0.6);

        var second = await MergeAsync(sourceId, other, "Mu Trust", new DateOnly(2026, 9, 1));

        Assert.Equal(MergeKind.PossibleDuplicate, second.Outcome.Kind);
        Assert.NotEqual(first.Outcome.BursaryId, second.Outcome.BursaryId);
        await using var db = fixture.CreateContext();
        var newBursary = await db.Bursaries.Include(b => b.Sources).SingleAsync(b => b.Id == second.Outcome.BursaryId);
        Assert.False(newBursary.IsPublished);
        Assert.Single(newBursary.Sources);
        Assert.Single((await db.Bursaries.Include(b => b.Sources).SingleAsync(b => b.Id == first.Outcome.BursaryId)).Sources);
        var item = await db.ReviewItems.SingleAsync(r => r.BursaryId == second.Outcome.BursaryId);
        Assert.Equal(ReviewItemType.PossibleDuplicate, item.Type);
        Assert.Contains(first.Outcome.BursaryId.ToString(), item.Details!.RootElement.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MergeAsync_NoFunderEitherSideAndVerySimilar_PossibleDuplicate()
    {
        var sourceId = await CreateSourceAsync();
        var first = await MergeAsync(sourceId, "Ndlovukazi Marine Biology Award", null, null);
        var second = await MergeAsync(sourceId, "Ndlovukazi Marine Biology Award", null, null);

        Assert.Equal(MergeKind.PossibleDuplicate, second.Outcome.Kind);
        Assert.NotEqual(first.Outcome.BursaryId, second.Outcome.BursaryId);
    }

    [Fact]
    public async Task MergeAsync_LockedClosingDate_IsNotOverwritten()
    {
        var sourceId = await CreateSourceAsync();
        var created = await MergeAsync(sourceId, "Bhekithemba Forestry Excellence", "Nu Trust", new DateOnly(2026, 9, 30));
        await using (var db = fixture.CreateContext())
        {
            var b = await db.Bursaries.SingleAsync(x => x.Id == created.Outcome.BursaryId);
            b.LockedFields = ["ClosingDate"];
            await db.SaveChangesAsync();
        }

        var merged = await MergeAsync(sourceId, "Bhekithemba Forestry Excellence", "Nu Trust", new DateOnly(2026, 10, 20), minAverage: 80m);

        Assert.Equal(MergeKind.Merged, merged.Outcome.Kind);
        await using var verify = fixture.CreateContext();
        var bursary = await verify.Bursaries.SingleAsync(x => x.Id == created.Outcome.BursaryId);
        Assert.Equal(new DateOnly(2026, 9, 30), bursary.ClosingDate);
        Assert.Equal(80m, bursary.MinAverage);
    }

    [Fact]
    public async Task MergeAsync_LockedStatus_IsNotOverwritten()
    {
        var sourceId = await CreateSourceAsync();
        var created = await MergeAsync(sourceId, "Sibusiso Astronomy Scholarship", "Pi Trust", new DateOnly(2026, 12, 30));
        await using (var db = fixture.CreateContext())
        {
            var b = await db.Bursaries.SingleAsync(x => x.Id == created.Outcome.BursaryId);
            b.Status = BursaryStatus.Closed;
            b.LockedFields = ["Status"];
            await db.SaveChangesAsync();
        }

        var merged = await MergeAsync(sourceId, "Sibusiso Astronomy Scholarship", "Pi Trust", new DateOnly(2026, 12, 30), minAverage: 70m);

        Assert.Equal(MergeKind.Merged, merged.Outcome.Kind);
        await using var verify = fixture.CreateContext();
        var bursary = await verify.Bursaries.SingleAsync(x => x.Id == created.Outcome.BursaryId);
        Assert.Equal(BursaryStatus.Closed, bursary.Status);
        Assert.Equal(70m, bursary.MinAverage);
    }

    [Fact]
    public async Task MergeAsync_LowConfidence_AddsOneReviewItemAcrossRepeatedMerges()
    {
        var sourceId = await CreateSourceAsync();
        var created = await MergeAsync(sourceId, "Zamokuhle Geology Prize", "Xi Trust", new DateOnly(2026, 9, 30), confidence: 0.5m);
        var url = $"https://merge.test/{Guid.NewGuid():N}";
        var again = await MergeAsync(sourceId, "Zamokuhle Geology Prize", "Xi Trust", new DateOnly(2026, 9, 30), confidence: 0.5m, url: url);
        var third = await MergeAsync(sourceId, "Zamokuhle Geology Prize", "Xi Trust", new DateOnly(2026, 9, 30), confidence: 0.5m);

        Assert.Equal(MergeKind.Merged, again.Outcome.Kind);
        Assert.Equal(MergeKind.Merged, third.Outcome.Kind);
        await using var db = fixture.CreateContext();
        var items = await db.ReviewItems.Where(r => r.BursaryId == created.Outcome.BursaryId).ToListAsync();
        Assert.Single(items, i => i.Type == ReviewItemType.LowConfidence);
        Assert.Single(items, i => i.Type == ReviewItemType.NewListing);
    }

    [Fact]
    public async Task MergeAsync_SameRawListingAgain_UpdatesSameBursaryAndSourceKeepsFirstSeen()
    {
        var sourceId = await CreateSourceAsync();
        var url = $"https://merge.test/{Guid.NewGuid():N}";
        var created = await MergeAsync(sourceId, "Omicron Hydrology Fellowship", null, null, url: url);

        _clock.UtcNow = T0.AddDays(5);
        var again = await MergeAsync(sourceId, "Omicron Hydrology Fellowship", null, null, url: url, rawListingId: created.RawListingId);

        Assert.Equal(MergeKind.Merged, again.Outcome.Kind);
        Assert.Equal(created.Outcome.BursaryId, again.Outcome.BursaryId);
        await using var db = fixture.CreateContext();
        var link = await db.BursarySources.SingleAsync(s => s.BursaryId == created.Outcome.BursaryId);
        Assert.Equal(T0, link.FirstSeenAt);
        Assert.Equal(T0.AddDays(5), link.LastSeenAt);
    }

    [Fact]
    public async Task MergeAsync_UnchangedTextWithLowerConfidence_DoesNotDowngrade()
    {
        var sourceId = await CreateSourceAsync();
        var url = $"https://merge.test/{Guid.NewGuid():N}";
        var created = await MergeAsync(sourceId, "Pi Astronomy Scholarship", "Rho Trust", null, confidence: 0.9m, url: url);
        await using (var db = fixture.CreateContext())
        {
            var row = await db.RawListings.SingleAsync(r => r.Id == created.RawListingId);
            row.LastFetchedAt = T0.AddDays(1);
            await db.SaveChangesAsync();
        }

        await MergeAsync(sourceId, "Pi Astronomy Scholarship", "Rho Trust", null, confidence: 0.4m, url: url, rawListingId: created.RawListingId);

        await using var verify = fixture.CreateContext();
        Assert.Equal(0.9m, (await verify.Bursaries.SingleAsync(b => b.Id == created.Outcome.BursaryId)).ExtractionConfidence);
    }

    [Fact]
    public async Task MergeAsync_QualificationsAndInstitutions_AreLinkedAndUnknownNamesIgnored()
    {
        var sourceId = await CreateSourceAsync();
        Guid instId, otherInstId, qualId;
        var tag = Guid.NewGuid().ToString("N")[..8];
        await using (var db = fixture.CreateContext())
        {
            var inst = new Institution { Name = "Inst " + tag, ShortName = "I" + tag, Slug = "i-" + tag, Type = InstitutionType.TraditionalUniversity, Province = Province.Gauteng, WebsiteUrl = "https://i.test" };
            var other = new Institution { Name = "Other " + tag, ShortName = "O" + tag, Slug = "o-" + tag, Type = InstitutionType.TraditionalUniversity, Province = Province.Gauteng, WebsiteUrl = "https://o.test" };
            var faculty = new Faculty { Institution = inst, Name = "Eng", Slug = "eng" };
            var otherFaculty = new Faculty { Institution = other, Name = "Eng", Slug = "eng" };
            var qual = new Qualification { Faculty = faculty, Name = "BSc Sigma " + tag, Abbreviation = "BSc", StudyLevel = StudyLevel.Undergraduate, Slug = "sigma" };
            var otherQual = new Qualification { Faculty = otherFaculty, Name = "BSc Sigma " + tag, Abbreviation = "BSc", StudyLevel = StudyLevel.Undergraduate, Slug = "sigma" };
            db.AddRange(inst, other, faculty, otherFaculty, qual, otherQual);
            await db.SaveChangesAsync();
            instId = inst.Id;
            otherInstId = other.Id;
            qualId = qual.Id;
        }

        var result = await MergeAsync(
            sourceId, "Tau Surveying Prize", "Upsilon Trust", null,
            qualifications: ["bsc sigma " + tag.ToUpperInvariant(), "No Such Qualification"],
            institutions: ["i" + tag.ToUpperInvariant()]);

        await using var verify = fixture.CreateContext();
        var bursary = await verify.Bursaries.Include(b => b.Qualifications).Include(b => b.Institutions)
            .SingleAsync(b => b.Id == result.Outcome.BursaryId);
        Assert.Equal(qualId, Assert.Single(bursary.Qualifications).QualificationId);
        Assert.Equal(instId, Assert.Single(bursary.Institutions).InstitutionId);
        Assert.NotEqual(otherInstId, bursary.Institutions.Single().InstitutionId);
    }

    [Fact]
    public async Task MergeAsync_EligibilityLocked_KeepsExistingLinks()
    {
        var sourceId = await CreateSourceAsync();
        var tag = Guid.NewGuid().ToString("N")[..8];
        await using (var db = fixture.CreateContext())
        {
            db.Institutions.Add(new Institution { Name = "Lock " + tag, ShortName = "L" + tag, Slug = "l-" + tag, Type = InstitutionType.Tvet, Province = Province.WesternCape, WebsiteUrl = "https://l.test" });
            await db.SaveChangesAsync();
        }

        var created = await MergeAsync(sourceId, "Phi Cartography Prize", "Chi Trust", null, institutions: ["L" + tag]);
        await using (var db = fixture.CreateContext())
        {
            var b = await db.Bursaries.SingleAsync(x => x.Id == created.Outcome.BursaryId);
            b.LockedFields = ["Eligibility"];
            await db.SaveChangesAsync();
        }

        await MergeAsync(sourceId, "Phi Cartography Prize", "Chi Trust", null, institutions: []);

        await using var verify = fixture.CreateContext();
        Assert.Single(await verify.BursaryInstitutions.Where(i => i.BursaryId == created.Outcome.BursaryId).ToListAsync());
    }
}
