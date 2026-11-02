using GetABursary.Core.Common;
using GetABursary.Core.Data;
using GetABursary.Core.Domain.Common;
using GetABursary.Core.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Tests.Data;

[Collection("Postgres")]
public class DbContextTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Migrations_Apply_And_PgTrgm_Exists()
    {
        await using var context = fixture.CreateContext();

        var pending = await context.Database.GetPendingMigrationsAsync();
        var extensions = await context.Database
            .SqlQueryRaw<string>("SELECT extname AS \"Value\" FROM pg_extension WHERE extname = 'pg_trgm'")
            .ToListAsync();

        Assert.Empty(pending);
        Assert.Single(extensions);
    }

    [Fact]
    public async Task SaveChanges_Sets_Audit_Timestamps()
    {
        var created = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(created);
        var options = new DbContextOptionsBuilder<GetABursaryDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var context = new ProbeDbContext(options, clock);
        await context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS audit_probe (id uuid PRIMARY KEY, created_at timestamptz NOT NULL, updated_at timestamptz NOT NULL)");

        var probe = new AuditProbe();
        context.Add(probe);
        await context.SaveChangesAsync();

        Assert.Equal(created, probe.CreatedAt);
        Assert.Equal(created, probe.UpdatedAt);

        clock.UtcNow = created.AddHours(1);
        context.Entry(probe).State = EntityState.Modified;
        await context.SaveChangesAsync();

        Assert.Equal(created, probe.CreatedAt);
        Assert.Equal(created.AddHours(1), probe.UpdatedAt);
    }
}

internal sealed class AuditProbe : Entity;

internal sealed class ProbeDbContext(DbContextOptions<GetABursaryDbContext> options, IClock clock)
    : GetABursaryDbContext(options, clock)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<AuditProbe>().ToTable("audit_probe");
    }
}

internal sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;

    public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}
