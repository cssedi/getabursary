using GetABursary.Core.Common;
using GetABursary.Core.Data;
using GetABursary.Core.Data.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace GetABursary.Api.Tests.Taxonomy;

/// <summary>
/// Starts one PostgreSQL container, applies migrations and loads the repository seed data (<c>data/seed</c>) once
/// per test class. The taxonomy endpoint tests only read, so they share this database.
/// </summary>
public sealed class TaxonomyPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var seeder = new DataSeeder(context, RepositorySeedDirectory(), NullLogger<DataSeeder>.Instance);
        await seeder.SeedAsync(CancellationToken.None);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>A new API host backed by this fixture's database.</summary>
    public WebApplicationFactory<Program> CreateApiFactory() => new TaxonomyPostgresApiFactory(ConnectionString);

    /// <summary>Opens a context for direct queries. Dispose it when done.</summary>
    public GetABursaryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GetABursaryDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options, new SystemClock());

    private static string RepositorySeedDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "data", "seed");
            if (File.Exists(Path.Combine(candidate, "fields-of-study.json")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not find data/seed above the test output directory.");
    }

    private sealed class TaxonomyPostgresApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("ConnectionStrings:Default", connectionString);
            builder.UseSetting("Reports:IpHashSalt", "taxonomy-test-salt");
        }
    }
}

[CollectionDefinition(Name)]
public sealed class TaxonomyPostgresFixtureDefinition : ICollectionFixture<TaxonomyPostgresFixture>
{
    public const string Name = "TaxonomyPostgres";
}
