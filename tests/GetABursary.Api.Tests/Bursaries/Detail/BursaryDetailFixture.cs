using GetABursary.Api.Tests.Bursaries.Search;
using GetABursary.Core.Common;
using GetABursary.Core.Data;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Taxonomy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace GetABursary.Api.Tests.Bursaries.Detail;

/// <summary>One PostgreSQL container holding an open, a closed and an unpublished bursary sharing one taxonomy.</summary>
public sealed class BursaryDetailFixture : IAsyncLifetime
{
    public const string OpenSlug = "open-chem-eng";
    public const string ClosedSlug = "closed-chem-eng";
    public const string UnpublishedSlug = "unpublished-chem-eng";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedAsync(context);

        Factory = new DetailApiFactory(_container.GetConnectionString());
        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    private GetABursaryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GetABursaryDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options, new FixedClock());

    private static async Task SeedAsync(GetABursaryDbContext db)
    {
        var field = new FieldOfStudy { Code = "0801", Name = "Chemical Engineering", Slug = "chemical-engineering" };
        var institution = new Institution
        {
            Name = "University of Cape Town",
            ShortName = "UCT",
            Slug = "uct",
            Type = InstitutionType.TraditionalUniversity,
            Province = Province.WesternCape,
            WebsiteUrl = "https://uct.example.test",
        };
        db.FieldsOfStudy.Add(field);
        db.Institutions.Add(institution);
        await db.SaveChangesAsync();

        var faculty = new Faculty { InstitutionId = institution.Id, Name = "Engineering", Slug = "eng" };
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync();

        var qualification = new Qualification
        {
            FacultyId = faculty.Id,
            Name = "BSc (Eng) Chemical Engineering",
            Abbreviation = "BScEng",
            StudyLevel = StudyLevel.Undergraduate,
            Slug = "chem-eng",
        };
        db.Qualifications.Add(qualification);
        await db.SaveChangesAsync();

        var funder = new Funder { Name = "Acme Foundation", NormalizedName = "acme foundation" };
        var zab = new Source { Key = "zabursaries", Name = "ZA Bursaries", BaseUrl = "https://za.example.test", IconUrl = "https://za.example.test/icon.png" };
        var trust = new Source { Key = "studytrust", Name = "Study Trust", BaseUrl = "https://trust.example.test" };
        db.Funders.Add(funder);
        db.Sources.AddRange(zab, trust);
        await db.SaveChangesAsync();

        var open = NewBursary(OpenSlug, "Open Chemical Engineering Bursary", funder.Id);
        open.ValueSummary = "R50 000 a year";
        open.WorkBackRequired = true;
        open.WorkBackDetails = "Work two years for the funder";
        open.MinAverage = 60m;
        open.MinSubjectRequirements = [new SubjectRequirement("Mathematics", 60m)];
        open.Citizenship = Citizenship.SouthAfrican;
        open.Provinces = [Province.WesternCape];
        open.ClosingDate = new DateOnly(2099, 12, 31);
        open.Fields.Add(new BursaryField { FieldOfStudy = field });
        open.Qualifications.Add(new BursaryQualification { Qualification = qualification });
        open.Institutions.Add(new BursaryInstitution { Institution = institution });
        open.Sources.Add(new BursarySource
        {
            Source = zab,
            SourceUrl = "https://za.example.test/open",
            FirstSeenAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            LastSeenAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
        });
        open.Sources.Add(new BursarySource
        {
            Source = trust,
            SourceUrl = "https://trust.example.test/open",
            FirstSeenAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            LastSeenAt = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero),
        });

        var closed = NewBursary(ClosedSlug, "Closed Chemical Engineering Bursary", funder.Id);
        closed.Status = BursaryStatus.Closed;
        closed.ClosingDate = new DateOnly(2026, 1, 31);
        closed.MinSubjectRequirements = [new SubjectRequirement("Physical Sciences", 70m)];
        closed.Fields.Add(new BursaryField { FieldOfStudy = field });
        closed.Qualifications.Add(new BursaryQualification { Qualification = qualification });
        closed.Institutions.Add(new BursaryInstitution { Institution = institution });
        closed.Sources.Add(new BursarySource
        {
            Source = zab,
            SourceUrl = "https://za.example.test/closed",
            FirstSeenAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            LastSeenAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        });

        var unpublished = NewBursary(UnpublishedSlug, "Unpublished Chemical Engineering Bursary", funder.Id);
        unpublished.IsPublished = false;
        unpublished.ClosingDate = new DateOnly(2099, 12, 31);

        db.Bursaries.AddRange(open, closed, unpublished);
        await db.SaveChangesAsync();
    }

    private static Bursary NewBursary(string slug, string name, Guid funderId) => new()
    {
        Slug = slug,
        Name = name,
        NormalizedName = name.ToLowerInvariant(),
        FunderId = funderId,
        Summary = "Summary text",
        Coverage = [CoverageItem.Tuition],
        StudyLevels = [StudyLevel.Undergraduate],
        Status = BursaryStatus.Open,
        IsPublished = true,
    };

    private sealed class DetailApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("ConnectionStrings:Default", connectionString);
            builder.UseSetting("Reports:IpHashSalt", "detail-test-salt");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IClock>();
                services.AddSingleton<IClock, FixedClock>();
            });
        }
    }
}

[CollectionDefinition(Name)]
public sealed class BursaryDetailFixtureDefinition : ICollectionFixture<BursaryDetailFixture>
{
    public const string Name = "BursaryDetail";
}
