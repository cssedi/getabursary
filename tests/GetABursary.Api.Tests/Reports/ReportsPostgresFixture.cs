using GetABursary.Core.Common;
using GetABursary.Core.Data;
using GetABursary.Core.Domain.Bursaries;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GetABursary.Api.Tests.Reports;

/// <summary>
/// Starts a throwaway PostgreSQL container, applies migrations once per test class, and hands out
/// API hosts wired to that database so report endpoint tests can assert on real rows.
/// </summary>
public sealed class ReportsPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>A new host with its own rate-limit counters, backed by this fixture's database.</summary>
    public WebApplicationFactory<Program> CreateApiFactory() => new ReportsPostgresApiFactory(ConnectionString);

    /// <summary>Opens a context for direct row assertions. Dispose it when done.</summary>
    public GetABursaryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GetABursaryDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options, new SystemClock());

    /// <summary>Inserts a minimal bursary and returns its id.</summary>
    public async Task<Guid> SeedBursaryAsync()
    {
        await using var context = CreateContext();
        var id = Guid.CreateVersion7();
        context.Bursaries.Add(new Bursary
        {
            Id = id,
            Slug = $"test-{id:N}",
            Name = "Test Bursary",
            NormalizedName = $"test bursary {id:N}",
        });
        await context.SaveChangesAsync();
        return id;
    }

    private sealed class ReportsPostgresApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("ConnectionStrings:Default", connectionString);
            builder.UseSetting("Reports:IpHashSalt", "postgres-test-salt");
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ReportsPostgresFixtureDefinition : ICollectionFixture<ReportsPostgresFixture>
{
    public const string Name = "ReportsPostgres";
}
