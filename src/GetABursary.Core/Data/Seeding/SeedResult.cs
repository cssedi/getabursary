namespace GetABursary.Core.Data.Seeding;

/// <summary>Row counts for one seed file. <paramref name="File"/> is relative to the seed folder, for example <c>faculties/uct.json</c>.</summary>
public sealed record SeedFileResult(string File, int Inserted, int Updated);

/// <summary>Outcome of one <see cref="IDataSeeder.SeedAsync"/> run, in the order the files were processed.</summary>
public sealed record SeedResult(IReadOnlyList<SeedFileResult> Files);
