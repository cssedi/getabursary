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
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace GetABursary.Api.Tests.Bursaries.Search;

/// <summary>A clock frozen at <see cref="Today"/> so closing-soon and date-range tests are deterministic.</summary>
public sealed class FixedClock : IClock
{
    public static readonly DateOnly TodayDate = new(2026, 6, 1);

    public DateTimeOffset UtcNow { get; } = new(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);

    public DateOnly Today => TodayDate;
}

/// <summary>Collects every SQL command EF Core logs, so a test can assert the number of round trips.</summary>
public sealed class SqlCapture : ILoggerProvider
{
    private readonly List<string> _commands = [];

    public IReadOnlyList<string> Commands
    {
        get
        {
            lock (_commands)
            {
                return [.. _commands];
            }
        }
    }

    public void Clear()
    {
        lock (_commands)
        {
            _commands.Clear();
        }
    }

    public ILogger CreateLogger(string categoryName) =>
        categoryName == "Microsoft.EntityFrameworkCore.Database.Command" ? new CommandLogger(this) : NullLogger.Instance;

    public void Dispose()
    {
    }

    private sealed class CommandLogger(SqlCapture owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel != LogLevel.Information)
            {
                return;
            }

            lock (owner._commands)
            {
                owner._commands.Add(formatter(state, exception));
            }
        }
    }

    private sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}

/// <summary>
/// One PostgreSQL container with a small hand-built dataset covering every matching rule
/// (see <see cref="Ids"/> and the comments in <see cref="SeedAsync"/>).
/// </summary>
public sealed class BursarySearchFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public SqlCapture Sql { get; } = new();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedAsync(context);

        Factory = new SearchApiFactory(_container.GetConnectionString(), Sql);
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
        var f08 = new FieldOfStudy { Id = Ids.Engineering, Code = "08", Name = "Engineering", Slug = "engineering" };
        var f0801 = new FieldOfStudy { Id = Ids.ChemicalEngineering, Code = "0801", Name = "Chemical Engineering", Slug = "chemical-engineering", ParentId = Ids.Engineering };
        var f13 = new FieldOfStudy { Id = Ids.LifeSciences, Code = "13", Name = "Life Sciences", Slug = "life-sciences" };
        db.FieldsOfStudy.AddRange(f08, f0801, f13);

        var uct = Institution(Ids.Uct, "University of Cape Town", "UCT");
        var wits = Institution(Ids.Wits, "University of the Witwatersrand", "Wits");
        db.Institutions.AddRange(uct, wits);

        db.Faculties.AddRange(
            new Faculty { Id = Ids.UctFaculty, InstitutionId = Ids.Uct, Name = "Engineering", Slug = "eng" },
            new Faculty { Id = Ids.WitsFaculty, InstitutionId = Ids.Wits, Name = "Engineering", Slug = "eng" });

        db.Qualifications.AddRange(
            new Qualification { Id = Ids.UctChemEng, FacultyId = Ids.UctFaculty, Name = "BSc (Eng) Chemical Engineering", Abbreviation = "BScEng", StudyLevel = StudyLevel.Undergraduate, Slug = "chem-eng" },
            new Qualification { Id = Ids.WitsChemEng, FacultyId = Ids.WitsFaculty, Name = "BSc (Eng) Chemical Engineering", Abbreviation = "BScEng", StudyLevel = StudyLevel.Undergraduate, Slug = "chem-eng" });
        await db.SaveChangesAsync();

        // The UCT qualification covers second-order 0801 (its parent 08 is added when resolving).
        db.QualificationFields.AddRange(
            new QualificationField { QualificationId = Ids.UctChemEng, FieldOfStudyId = Ids.ChemicalEngineering },
            new QualificationField { QualificationId = Ids.WitsChemEng, FieldOfStudyId = Ids.ChemicalEngineering });

        var funder = new Funder { Name = "Acme Foundation", NormalizedName = "acme foundation" };
        db.Funders.Add(funder);

        var zab = new Source { Id = Ids.SourceZa, Key = "zabursaries", Name = "ZA Bursaries", BaseUrl = "https://za.example.test", IconUrl = "https://za.example.test/icon.png" };
        var trust = new Source { Id = Ids.SourceTrust, Key = "studytrust", Name = "Study Trust", BaseUrl = "https://trust.example.test", IconUrl = null };
        db.Sources.AddRange(zab, trust);
        await db.SaveChangesAsync();

        var today = FixedClock.TodayDate;
        var undergrad = new List<StudyLevel> { StudyLevel.Undergraduate };

        // Expected for UCT chem-eng, Undergraduate, average 70: Qual(3) > SecondOrder(2) > UnknownLevel(1.5) > FirstOrder(1) > AllFields(0.5)
        var qual = Bursary("Qualification Match", undergrad, today.AddDays(30), funder.Id, minAverage: 60, allFields: true);
        var second = Bursary("Second Order Field", undergrad, today.AddDays(10));
        var first = Bursary("First Order Field", undergrad, today.AddDays(60));
        var all = Bursary("All Fields", undergrad, today.AddDays(90), allFields: true);
        var unknownLevel = Bursary("Unknown Level", [], null);
        var witsOnly = Bursary("Wits Only", undergrad, today.AddDays(20));
        var highMin = Bursary("High Minimum", undergrad, today.AddDays(20), minAverage: 80);
        var closed = Bursary("Closed Bursary", undergrad, today.AddDays(-10), status: BursaryStatus.Closed);
        var unrelated = Bursary("Unrelated Field", undergrad, today.AddDays(20));
        var unpublished = Bursary("Unpublished", undergrad, today.AddDays(20), published: false);
        var honours = Bursary("Honours Only", [StudyLevel.Honours], today.AddDays(20));
        db.Bursaries.AddRange(qual, second, first, all, unknownLevel, witsOnly, highMin, closed, unrelated, unpublished, honours);
        await db.SaveChangesAsync();

        db.BursaryQualifications.Add(new BursaryQualification { BursaryId = qual.Id, QualificationId = Ids.UctChemEng });
        db.BursaryFields.AddRange(
            Field(second, Ids.ChemicalEngineering),
            Field(first, Ids.Engineering),
            Field(unknownLevel, Ids.ChemicalEngineering),
            Field(witsOnly, Ids.ChemicalEngineering),
            Field(highMin, Ids.ChemicalEngineering),
            Field(closed, Ids.ChemicalEngineering),
            Field(unrelated, Ids.LifeSciences),
            Field(unpublished, Ids.ChemicalEngineering),
            Field(honours, Ids.ChemicalEngineering));
        db.BursaryInstitutions.Add(new BursaryInstitution { BursaryId = witsOnly.Id, InstitutionId = Ids.Wits });

        var seen = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        db.BursarySources.AddRange(
            new BursarySource { BursaryId = qual.Id, SourceId = Ids.SourceZa, SourceUrl = "https://za.example.test/qual", FirstSeenAt = seen, LastSeenAt = seen },
            new BursarySource { BursaryId = qual.Id, SourceId = Ids.SourceTrust, SourceUrl = "https://trust.example.test/qual", FirstSeenAt = seen, LastSeenAt = seen },
            new BursarySource { BursaryId = second.Id, SourceId = Ids.SourceZa, SourceUrl = "https://za.example.test/second", FirstSeenAt = seen, LastSeenAt = seen });
        await db.SaveChangesAsync();
    }

    private static Institution Institution(Guid id, string name, string shortName) => new()
    {
        Id = id,
        Name = name,
        ShortName = shortName,
        Slug = shortName.ToLowerInvariant(),
        Type = InstitutionType.TraditionalUniversity,
        Province = Province.WesternCape,
        WebsiteUrl = "https://example.test",
    };

    private static BursaryField Field(Bursary bursary, Guid fieldId) => new() { BursaryId = bursary.Id, FieldOfStudyId = fieldId };

    private static Bursary Bursary(
        string name,
        List<StudyLevel> levels,
        DateOnly? closing,
        Guid? funderId = null,
        decimal? minAverage = null,
        bool allFields = false,
        BursaryStatus status = BursaryStatus.Open,
        bool published = true) => new()
    {
        Slug = name.ToLowerInvariant().Replace(' ', '-'),
        Name = name,
        NormalizedName = name.ToLowerInvariant(),
        FunderId = funderId,
        StudyLevels = levels,
        MinAverage = minAverage,
        AllFieldsEligible = allFields,
        ClosingDate = closing,
        Status = closing is null ? BursaryStatus.Unknown : status,
        IsPublished = published,
        Coverage = [CoverageItem.Tuition],
    };

    /// <summary>Fixed identifiers for the seeded taxonomy.</summary>
    public static class Ids
    {
        public static readonly Guid Engineering = Guid.CreateVersion7();
        public static readonly Guid ChemicalEngineering = Guid.CreateVersion7();
        public static readonly Guid LifeSciences = Guid.CreateVersion7();
        public static readonly Guid Uct = Guid.CreateVersion7();
        public static readonly Guid Wits = Guid.CreateVersion7();
        public static readonly Guid UctFaculty = Guid.CreateVersion7();
        public static readonly Guid WitsFaculty = Guid.CreateVersion7();
        public static readonly Guid UctChemEng = Guid.CreateVersion7();
        public static readonly Guid WitsChemEng = Guid.CreateVersion7();
        public static readonly Guid SourceZa = Guid.CreateVersion7();
        public static readonly Guid SourceTrust = Guid.CreateVersion7();
    }

    private sealed class SearchApiFactory(string connectionString, SqlCapture sql) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("ConnectionStrings:Default", connectionString);
            builder.UseSetting("Reports:IpHashSalt", "search-test-salt");

            builder.ConfigureLogging(logging =>
            {
                logging.AddProvider(sql);
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IClock>();
                services.AddSingleton<IClock, FixedClock>();
            });
        }
    }
}

[CollectionDefinition(Name)]
public sealed class BursarySearchFixtureDefinition : ICollectionFixture<BursarySearchFixture>
{
    public const string Name = "BursarySearch";
}
