namespace GetABursary.Core.Data.Seeding;

/// <summary>Upserts the reference JSON under <c>data/seed</c> into the database. Safe to run any number of times.</summary>
public interface IDataSeeder
{
    Task<SeedResult> SeedAsync(CancellationToken ct);
}
