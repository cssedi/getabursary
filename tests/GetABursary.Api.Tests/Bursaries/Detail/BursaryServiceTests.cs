using GetABursary.Api.DTOs.Bursaries;
using GetABursary.Api.Services.Bursaries;
using GetABursary.Api.Tests.Bursaries.Search;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Taxonomy;

namespace GetABursary.Api.Tests.Bursaries.Detail;

public class BursaryServiceTests
{
    private const string Slug = "uct-chem-eng";

    [Fact]
    public async Task GetDetailAsync_UnknownSlug_ReturnsNotFound()
    {
        var service = NewService(new FakeBursaryReadRepository());

        var result = await service.GetDetailAsync("no-such-bursary", CancellationToken.None);

        AssertSingleError(result, "Bursary.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task GetDetailAsync_UnpublishedBursary_ReturnsNotFoundEvenWhenOpen()
    {
        var repository = new FakeBursaryReadRepository();
        repository.BySlug[Slug] = NewBursary(published: false);
        var service = NewService(repository);

        var result = await service.GetDetailAsync(Slug, CancellationToken.None);

        AssertSingleError(result, "Bursary.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task GetDetailAsync_ClosedBursary_ReturnsSummaryOnlyWithoutDetail()
    {
        var repository = new FakeBursaryReadRepository();
        repository.BySlug[Slug] = NewBursary(status: BursaryStatus.Closed);
        var service = NewService(repository);

        var result = await service.GetDetailAsync(Slug, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Detail);
        Assert.NotNull(result.Value.ClosedSummary);
        Assert.Equal("Closed", result.Value.ClosedSummary.Status);
        Assert.Equal("Acme Foundation", result.Value.ClosedSummary.FunderName);
    }

    [Fact]
    public async Task GetDetailAsync_OpenBursary_MapsEligibilityFieldsQualificationsAndInstitutions()
    {
        var repository = new FakeBursaryReadRepository();
        repository.BySlug[Slug] = NewBursary(applyUrl: "https://funder.example.test/apply");
        var service = NewService(repository);

        var result = await service.GetDetailAsync(Slug, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ClosedSummary);
        var detail = result.Value.Detail!;
        Assert.Equal(Slug, detail.Slug);
        Assert.Equal("Open", detail.Status);
        Assert.True(detail.IsClosingSoon);
        Assert.Equal(["Undergraduate"], detail.StudyLevels);
        Assert.Equal(["Tuition"], detail.Coverage);
        Assert.Equal("Summary text", detail.Summary);
        Assert.Equal("Work two years", detail.WorkBackDetails);
        Assert.Equal([new SubjectRequirementResponse("Mathematics", 60m)], detail.MinSubjectRequirements);
        Assert.Equal("SouthAfrican", detail.Citizenship);
        Assert.Equal(["WesternCape"], detail.Provinces);
        Assert.Equal([new BursaryFieldResponse("0801", "Chemical Engineering")], detail.Fields);
        Assert.Equal([new BursaryQualificationResponse(QualificationId, "BSc (Eng) Chemical Engineering", "UCT")], detail.Qualifications);
        Assert.Equal([new BursaryInstitutionResponse(InstitutionId, "UCT")], detail.Institutions);
        Assert.Equal(new DateOnly(2026, 6, 10), detail.ClosingDate);
        Assert.Equal(new DateOnly(2026, 5, 1), detail.OpenDate);
        Assert.Equal(new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero), detail.UpdatedAt);
    }

    [Fact]
    public async Task GetDetailAsync_NoApplyUrl_FallsBackToMostRecentlySeenSource()
    {
        var repository = new FakeBursaryReadRepository();
        repository.BySlug[Slug] = NewBursary(applyUrl: null);
        var service = NewService(repository);

        var result = await service.GetDetailAsync(Slug, CancellationToken.None);

        Assert.Equal("https://trust.example.test/uct", result.Value.Detail!.ApplyUrl);
    }

    [Fact]
    public async Task GetDetailAsync_ApplyUrlSet_KeepsFunderLinkOverSourceListing()
    {
        var repository = new FakeBursaryReadRepository();
        repository.BySlug[Slug] = NewBursary(applyUrl: "https://funder.example.test/apply");
        var service = NewService(repository);

        var result = await service.GetDetailAsync(Slug, CancellationToken.None);

        Assert.Equal("https://funder.example.test/apply", result.Value.Detail!.ApplyUrl);
    }

    private static readonly Guid QualificationId = Guid.CreateVersion7();
    private static readonly Guid InstitutionId = Guid.CreateVersion7();

    private static BursaryService NewService(FakeBursaryReadRepository repository)
    {
        var clock = new FixedClock();
        return new BursaryService(repository, clock);
    }

    /// <summary>An open, published bursary closing 2026-06-10 (inside the 14-day window from <see cref="Today"/>), with two sources.</summary>
    private static Bursary NewBursary(
        bool published = true,
        BursaryStatus status = BursaryStatus.Open,
        string? applyUrl = null)
    {
        var institution = new Institution
        {
            Id = InstitutionId,
            Name = "University of Cape Town",
            ShortName = "UCT",
            Slug = "uct",
            Type = InstitutionType.TraditionalUniversity,
            Province = Province.WesternCape,
            WebsiteUrl = "https://uct.example.test",
        };
        var faculty = new Faculty { Name = "Engineering", Slug = "eng", Institution = institution };
        var qualification = new Qualification
        {
            Id = QualificationId,
            Name = "BSc (Eng) Chemical Engineering",
            Abbreviation = "BScEng",
            StudyLevel = StudyLevel.Undergraduate,
            Slug = "chem-eng",
            Faculty = faculty,
        };
        var funder = new Funder { Name = "Acme Foundation", NormalizedName = "acme foundation" };
        var zabursaries = new Source { Key = "zabursaries", Name = "ZA Bursaries", BaseUrl = "https://za.example.test" };
        var studyTrust = new Source { Key = "studytrust", Name = "Study Trust", BaseUrl = "https://trust.example.test" };
        var field = new FieldOfStudy { Code = "0801", Name = "Chemical Engineering", Slug = "chem-eng-field" };

        var bursary = new Bursary
        {
            Slug = Slug,
            Name = "UCT Chemical Engineering Bursary",
            NormalizedName = "uct chemical engineering",
            Funder = funder,
            FunderId = funder.Id,
            Summary = "Summary text",
            ValueSummary = "R50 000 a year",
            Coverage = [CoverageItem.Tuition],
            WorkBackRequired = true,
            WorkBackDetails = "Work two years",
            StudyLevels = [StudyLevel.Undergraduate],
            MinAverage = 60m,
            MinSubjectRequirements = [new SubjectRequirement("Mathematics", 60m)],
            Citizenship = Citizenship.SouthAfrican,
            Provinces = [Province.WesternCape],
            OpenDate = new DateOnly(2026, 5, 1),
            ClosingDate = new DateOnly(2026, 6, 10),
            Status = status,
            ApplyUrl = applyUrl,
            IsPublished = published,
            UpdatedAt = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero),
        };

        bursary.Fields.Add(new BursaryField { Bursary = bursary, FieldOfStudy = field });
        bursary.Qualifications.Add(new BursaryQualification { Bursary = bursary, Qualification = qualification });
        bursary.Institutions.Add(new BursaryInstitution { Bursary = bursary, Institution = institution });
        bursary.Sources.Add(new BursarySource
        {
            Bursary = bursary,
            Source = zabursaries,
            SourceUrl = "https://za.example.test/uct",
            FirstSeenAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            LastSeenAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
        });
        bursary.Sources.Add(new BursarySource
        {
            Bursary = bursary,
            Source = studyTrust,
            SourceUrl = "https://trust.example.test/uct",
            FirstSeenAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            LastSeenAt = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero),
        });

        return bursary;
    }

    private static void AssertSingleError<T>(Result<T> result, string code, ErrorType type)
    {
        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(code, error.Code);
        Assert.Equal(type, error.Type);
    }
}
