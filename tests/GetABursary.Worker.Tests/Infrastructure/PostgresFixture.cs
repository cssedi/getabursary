using GetABursary.Core.Common;
using GetABursary.Core.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GetABursary.Worker.Tests.Infrastructure;

/// <summary>Starts a throwaway PostgreSQL container and applies migrations once per test collection.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public GetABursaryDbContext CreateContext(IClock? clock = null)
    {
        var options = new DbContextOptionsBuilder<GetABursaryDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        return new GetABursaryDbContext(options, clock ?? new SystemClock());
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition("Postgres")]
public sealed class PostgresFixtureDefinition : ICollectionFixture<PostgresFixture>;
