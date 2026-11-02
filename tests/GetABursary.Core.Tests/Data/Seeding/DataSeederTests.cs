using GetABursary.Core.Common;
using GetABursary.Core.Data;
using GetABursary.Core.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GetABursary.Core.Tests.Data.Seeding;

public sealed class DataSeederTests(SeedingPostgresFixture fixture) : IClassFixture<SeedingPostgresFixture>, IDisposable
{
    private static readonly string[] RootSeedFiles = ["fields-of-study.json", "institutions.json", "sources.json"];

    private readonly List<string> _tempDirectories = [];

    public void Dispose()
    {
        foreach (var directory in _tempDirectories)
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SeedAsync_EmptyDatabase_InsertsTwentyFieldsTwentySixInstitutionsAndNineSources()
    {
        var options = await fixture.CreateEmptyDatabaseAsync();

        var result = await RunSeederAsync(options, RepositorySeedDirectory());

        Assert.Equal(20, FileResult(result, "fields-of-study.json").Inserted);
        Assert.Equal(26, FileResult(result, "institutions.json").Inserted);
        Assert.Equal(9, FileResult(result, "sources.json").Inserted);

        await using var db = CreateContext(options);
        Assert.Equal(20, await db.FieldsOfStudy.CountAsync());
        Assert.Equal(26, await db.Institutions.CountAsync());
        Assert.Equal(9, await db.Sources.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_RunTwice_SecondRunInsertsAndUpdatesNothing()
    {
        var options = await fixture.CreateEmptyDatabaseAsync();
        var seedDirectory = CopySeedData(includeFaculties: true);

        await RunSeederAsync(options, seedDirectory);
        var second = await RunSeederAsync(options, seedDirectory);

        Assert.All(second.Files, file =>
        {
            Assert.Equal(0, file.Inserted);
            Assert.Equal(0, file.Updated);
        });
    }

    [Fact]
    public async Task SeedAsync_ChangedNameInSeedCopy_UpdatesOnlyThatInstitution()
    {
        var options = await fixture.CreateEmptyDatabaseAsync();
        var seedDirectory = CopySeedData(includeFaculties: true);
        await RunSeederAsync(options, seedDirectory);

        var institutionsPath = Path.Combine(seedDirectory, "institutions.json");
        var original = await File.ReadAllTextAsync(institutionsPath);
        const string oldName = "\"University of Cape Town\"";
        Assert.Contains(oldName, original, StringComparison.Ordinal);
        await File.WriteAllTextAsync(institutionsPath, original.Replace(oldName, "\"University of Cape Town (renamed)\"", StringComparison.Ordinal));

        var result = await RunSeederAsync(options, seedDirectory);

        Assert.Equal(1, FileResult(result, "institutions.json").Updated);
        Assert.Equal(0, FileResult(result, "institutions.json").Inserted);
        Assert.Equal(0, FileResult(result, "fields-of-study.json").Updated);
        Assert.Equal(0, FileResult(result, "sources.json").Updated);
        Assert.Equal(0, FileResult(result, "faculties/uct.json").Updated);

        await using var db = CreateContext(options);
        Assert.Equal("University of Cape Town (renamed)", (await db.Institutions.SingleAsync(i => i.Slug == "uct")).Name);
        Assert.Equal("University of the Witwatersrand", (await db.Institutions.SingleAsync(i => i.Slug == "wits")).Name);
        Assert.Equal(26, await db.Institutions.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_SourceEnabledSetInDatabase_StaysEnabledAfterReseed()
    {
        var options = await fixture.CreateEmptyDatabaseAsync();
        var seedDirectory = CopySeedData(includeFaculties: false);
        await RunSeederAsync(options, seedDirectory);

        await using (var admin = CreateContext(options))
        {
            var source = await admin.Sources.SingleAsync(s => s.Key == "zabursaries");
            source.Enabled = true;
            await admin.SaveChangesAsync();
        }

        var result = await RunSeederAsync(options, seedDirectory);

        Assert.Equal(0, FileResult(result, "sources.json").Updated);
        await using var db = CreateContext(options);
        Assert.True((await db.Sources.SingleAsync(s => s.Key == "zabursaries")).Enabled);
    }

    [Fact]
    public async Task SeedAsync_FacultiesFile_UpsertsFacultyAndReplacesFieldLinks()
    {
        var options = await fixture.CreateEmptyDatabaseAsync();
        var seedDirectory = CopySeedData(includeFaculties: false);
        WriteFaculties(seedDirectory, fieldCodes: "\"08\", \"04\"");

        await RunSeederAsync(options, seedDirectory);

        await using (var check = CreateContext(options))
        {
            var qualification = await check.Qualifications.Include(q => q.QualificationFields).SingleAsync(q => q.Slug == "bsc-test");
            Assert.Equal(2, qualification.QualificationFields.Count);
        }

        WriteFaculties(seedDirectory, fieldCodes: "\"08\"");
        var result = await RunSeederAsync(options, seedDirectory);

        Assert.Equal(1, FileResult(result, "faculties/test-uni.json").Updated);
        Assert.Equal(0, FileResult(result, "faculties/test-uni.json").Inserted);

        await using var db = CreateContext(options);
        var reloaded = await db.Qualifications.Include(q => q.QualificationFields).SingleAsync(q => q.Slug == "bsc-test");
        var link = Assert.Single(reloaded.QualificationFields);
        var engineering = await db.FieldsOfStudy.SingleAsync(f => f.Code == "08");
        Assert.Equal(engineering.Id, link.FieldOfStudyId);
        Assert.Equal(1, await db.Faculties.CountAsync(f => f.Slug == "test-faculty"));
    }

    [Fact]
    public async Task SeedAsync_NoFacultiesFolder_SkipsFacultyFiles()
    {
        var options = await fixture.CreateEmptyDatabaseAsync();
        var seedDirectory = CopySeedData(includeFaculties: false);

        var result = await RunSeederAsync(options, seedDirectory);

        Assert.DoesNotContain(result.Files, file => file.File.StartsWith("faculties/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SeedAsync_UnknownFieldCodeInFaculties_ThrowsInvalidDataException()
    {
        var options = await fixture.CreateEmptyDatabaseAsync();
        var seedDirectory = CopySeedData(includeFaculties: false);
        WriteFaculties(seedDirectory, fieldCodes: "\"99\"");

        var seeder = new DataSeeder(CreateContext(options), seedDirectory, NullLogger<DataSeeder>.Instance);

        await Assert.ThrowsAsync<InvalidDataException>(() => seeder.SeedAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SeedAsync_ChildListedBeforeParent_InsertsParentFirstAndLinksChild()
    {
        var options = await fixture.CreateEmptyDatabaseAsync();
        var seedDirectory = CopySeedData(includeFaculties: false);
        File.WriteAllText(Path.Combine(seedDirectory, "fields-of-study.json"), """
            [
              { "code": "0801", "name": "Chemical Engineering", "slug": "chemical-engineering", "parentCode": "08" },
              { "code": "08", "name": "Engineering", "slug": "engineering", "parentCode": null }
            ]
            """);

        await RunSeederAsync(options, seedDirectory);

        await using var db = CreateContext(options);
        var parent = await db.FieldsOfStudy.SingleAsync(f => f.Code == "08");
        var child = await db.FieldsOfStudy.SingleAsync(f => f.Code == "0801");
        Assert.Equal(parent.Id, child.ParentId);
    }

    private static GetABursaryDbContext CreateContext(DbContextOptions<GetABursaryDbContext> options) =>
        new(options, new SystemClock());

    private static async Task<SeedResult> RunSeederAsync(DbContextOptions<GetABursaryDbContext> options, string seedDirectory)
    {
        await using var db = CreateContext(options);
        var seeder = new DataSeeder(db, seedDirectory, NullLogger<DataSeeder>.Instance);
        return await seeder.SeedAsync(CancellationToken.None);
    }

    private static SeedFileResult FileResult(SeedResult result, string file) =>
        result.Files.Single(f => f.File == file);

    /// <summary>Walks up from the test output folder to the repository's <c>data/seed</c>.</summary>
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

    /// <summary>Copies the real reference files into a fresh temp folder that a test can edit.</summary>
    private string CopySeedData(bool includeFaculties)
    {
        var source = RepositorySeedDirectory();
        var target = NewTempDirectory();

        foreach (var file in RootSeedFiles)
        {
            File.Copy(Path.Combine(source, file), Path.Combine(target, file));
        }

        if (includeFaculties)
        {
            var facultiesTarget = Directory.CreateDirectory(Path.Combine(target, "faculties"));
            foreach (var file in Directory.GetFiles(Path.Combine(source, "faculties"), "*.json"))
            {
                File.Copy(file, Path.Combine(facultiesTarget.FullName, Path.GetFileName(file)));
            }
        }

        return target;
    }

    /// <summary>Writes a one-faculty file for UCT with one qualification linked to the given field codes.</summary>
    private static void WriteFaculties(string seedDirectory, string fieldCodes)
    {
        var folder = Directory.CreateDirectory(Path.Combine(seedDirectory, "faculties"));
        File.WriteAllText(Path.Combine(folder.FullName, "test-uni.json"), $$"""
            {
              "institutionSlug": "uct",
              "sourceUrls": ["https://uct.ac.za/faculties"],
              "faculties": [
                {
                  "name": "Test Faculty",
                  "slug": "test-faculty",
                  "qualifications": [
                    { "name": "Test Degree", "abbreviation": "TD", "studyLevel": "Undergraduate", "durationYears": 4, "slug": "bsc-test", "fieldCodes": [ {{fieldCodes}} ] }
                  ]
                }
              ]
            }
            """);
    }

    private string NewTempDirectory()
    {
        var path = Directory.CreateTempSubdirectory("seed-test-").FullName;
        _tempDirectories.Add(path);
        return path;
    }
}
