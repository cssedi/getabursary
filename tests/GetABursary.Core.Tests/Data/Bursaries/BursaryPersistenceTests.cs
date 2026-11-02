using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Taxonomy;
using GetABursary.Core.Tests.Data.Taxonomy;
using GetABursary.Core.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Tests.Data.Bursaries;

[Collection("Postgres")]
public class BursaryPersistenceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Bursary_Graph_Saves_And_Reloads_With_Every_Value_Intact()
    {
        var field1 = new FieldOfStudy { Code = TaxonomyPersistenceTests.UniqueCode(), Name = "Engineering", Slug = TaxonomyPersistenceTests.UniqueSlug() };
        var field2 = new FieldOfStudy { Code = TaxonomyPersistenceTests.UniqueCode(), Name = "Computing", Slug = TaxonomyPersistenceTests.UniqueSlug() };
        var institution = new Institution
        {
            Name = "University of Example",
            ShortName = "UEx",
            Slug = TaxonomyPersistenceTests.UniqueSlug(),
            Type = InstitutionType.TraditionalUniversity,
            Province = Province.Gauteng,
            WebsiteUrl = "https://example.test",
        };
        var funder = new Funder { Name = "Acme Trust", NormalizedName = "acme trust " + Guid.NewGuid().ToString("N") };
        var source1 = new Source { Key = "k" + Guid.NewGuid().ToString("N"), Name = "Source One", BaseUrl = "https://one.test" };
        var source2 = new Source { Key = "k" + Guid.NewGuid().ToString("N"), Name = "Source Two", BaseUrl = "https://two.test" };
        var seen = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);

        var bursary = new Bursary
        {
            Slug = TaxonomyPersistenceTests.UniqueSlug(),
            Name = "Acme Engineering Bursary 2027",
            NormalizedName = "acme engineering",
            Funder = funder,
            Summary = "A summary",
            ValueSummary = "Full cost of study",
            Coverage = [CoverageItem.Tuition, CoverageItem.Laptop, CoverageItem.Stipend],
            WorkBackRequired = true,
            WorkBackDetails = "1 year per year funded",
            StudyLevels = [StudyLevel.Undergraduate, StudyLevel.Honours],
            MinAverage = 65.5m,
            MinSubjectRequirements = [new SubjectRequirement("Mathematics", 70m), new SubjectRequirement("Physical Sciences", 60.5m)],
            Citizenship = Citizenship.SouthAfrican,
            Provinces = [Province.Gauteng, Province.WesternCape],
            AllFieldsEligible = false,
            OpenDate = new DateOnly(2026, 4, 1),
            ClosingDate = new DateOnly(2026, 9, 30),
            Status = BursaryStatus.Open,
            ApplyUrl = "https://apply.test",
            ExtractionConfidence = 0.87m,
            LockedFields = ["closing_date", "summary"],
            IsPublished = true,
            LastSeenAt = seen,
        };
        bursary.Fields.Add(new BursaryField { Bursary = bursary, FieldOfStudy = field1 });
        bursary.Fields.Add(new BursaryField { Bursary = bursary, FieldOfStudy = field2 });
        bursary.Institutions.Add(new BursaryInstitution { Bursary = bursary, Institution = institution });
        bursary.Sources.Add(new BursarySource { Bursary = bursary, Source = source1, SourceUrl = "https://one.test/a", FirstSeenAt = seen, LastSeenAt = seen });
        bursary.Sources.Add(new BursarySource { Bursary = bursary, Source = source2, SourceUrl = "https://two.test/a", FirstSeenAt = seen, LastSeenAt = seen });

        await using (var writer = fixture.CreateContext())
        {
            writer.Bursaries.Add(bursary);
            await writer.SaveChangesAsync();
        }

        await using var reader = fixture.CreateContext();
        var loaded = await reader.Bursaries
            .AsNoTracking()
            .Include(b => b.Funder)
            .Include(b => b.Fields)
            .Include(b => b.Institutions)
            .Include(b => b.Sources)
            .SingleAsync(b => b.Id == bursary.Id);

        Assert.Equal(bursary.Slug, loaded.Slug);
        Assert.Equal(bursary.Name, loaded.Name);
        Assert.Equal("acme engineering", loaded.NormalizedName);
        Assert.Equal(funder.Id, loaded.Funder?.Id);
        Assert.Equal("A summary", loaded.Summary);
        Assert.Equal("Full cost of study", loaded.ValueSummary);
        Assert.Equal(bursary.Coverage, loaded.Coverage);
        Assert.True(loaded.WorkBackRequired);
        Assert.Equal("1 year per year funded", loaded.WorkBackDetails);
        Assert.Equal(bursary.StudyLevels, loaded.StudyLevels);
        Assert.Equal(65.5m, loaded.MinAverage);
        Assert.Equal(bursary.MinSubjectRequirements, loaded.MinSubjectRequirements);
        Assert.Equal(Citizenship.SouthAfrican, loaded.Citizenship);
        Assert.Equal(bursary.Provinces, loaded.Provinces);
        Assert.False(loaded.AllFieldsEligible);
        Assert.Equal(new DateOnly(2026, 4, 1), loaded.OpenDate);
        Assert.Equal(new DateOnly(2026, 9, 30), loaded.ClosingDate);
        Assert.Equal(BursaryStatus.Open, loaded.Status);
        Assert.Equal("https://apply.test", loaded.ApplyUrl);
        Assert.Equal(0.87m, loaded.ExtractionConfidence);
        Assert.Equal(["closing_date", "summary"], loaded.LockedFields);
        Assert.True(loaded.IsPublished);
        Assert.Equal(seen, loaded.LastSeenAt);

        Assert.Equal(
            new[] { field1.Id, field2.Id }.Order(),
            loaded.Fields.Select(f => f.FieldOfStudyId).Order());
        Assert.Equal(institution.Id, Assert.Single(loaded.Institutions).InstitutionId);
        Assert.Equal(
            ["https://one.test/a", "https://two.test/a"],
            loaded.Sources.Select(s => s.SourceUrl).Order());
    }

    [Fact]
    public async Task NormalizedName_TrigramIndex_Exists_And_Similarity_Is_Queryable()
    {
        await using var context = fixture.CreateContext();

        var indexDefs = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT indexdef AS "Value" FROM pg_indexes
                WHERE tablename = 'bursary' AND indexdef ILIKE '%gin%' AND indexdef ILIKE '%gin_trgm_ops%'
                """)
            .ToListAsync();
        Assert.Contains(indexDefs, d => d.Contains("normalized_name", StringComparison.Ordinal));

        var name = "trgm " + Guid.NewGuid().ToString("N");
        context.Bursaries.Add(new Bursary { Slug = TaxonomyPersistenceTests.UniqueSlug(), Name = name, NormalizedName = name });
        await context.SaveChangesAsync();

        var scores = await context.Bursaries
            .Where(b => b.NormalizedName == name)
            .Select(b => EF.Functions.TrigramsSimilarity(b.NormalizedName, name))
            .ToListAsync();
        Assert.Equal(1f, Assert.Single(scores));

        var none = await context.Bursaries
            .Where(b => EF.Functions.TrigramsSimilarity(b.NormalizedName, "x") > 0.9)
            .CountAsync();
        Assert.Equal(0, none);
    }

    [Fact]
    public async Task Source_DuplicateKey_Throws()
    {
        var key = "k" + Guid.NewGuid().ToString("N");
        await using var context = fixture.CreateContext();
        context.Sources.Add(new Source { Key = key, Name = "A", BaseUrl = "https://a.test" });
        await context.SaveChangesAsync();

        await using var second = fixture.CreateContext();
        second.Sources.Add(new Source { Key = key, Name = "B", BaseUrl = "https://b.test" });
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }
}
