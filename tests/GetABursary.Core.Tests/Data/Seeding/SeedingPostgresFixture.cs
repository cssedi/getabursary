using GetABursary.Core.Common;
using GetABursary.Core.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace GetABursary.Core.Tests.Data.Seeding;

/// <summary>
/// Owns one PostgreSQL container for a test class. Each test gets its own empty, migrated database,
/// so counts are exact and tests cannot see each other's rows.
/// </summary>
public sealed class SeedingPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Creates a new empty database, applies migrations, and returns its options.</summary>
    public async Task<DbContextOptions<GetABursaryDbContext>> CreateEmptyDatabaseAsync()
    {
        var databaseName = "seed_" + Guid.CreateVersion7().ToString("N");
        var adminConnectionString = _container.GetConnectionString();

        await using (var admin = new NpgsqlConnection(adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        var connectionString = new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = databaseName }.ConnectionString;
        var options = new DbContextOptionsBuilder<GetABursaryDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var context = new GetABursaryDbContext(options, new SystemClock());
        await context.Database.MigrateAsync();

        return options;
    }
}
