using GetABursary.Core.Domain.Taxonomy;
using GetABursary.Core.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Tests.Data.Taxonomy;

[Collection("Postgres")]
public class FieldOfStudyConstraintTests(PostgresFixture fixture)
{
    [Fact]
    public async Task SaveChanges_DuplicateCode_ThrowsDbUpdateException()
    {
        var code = TaxonomyPersistenceTests.UniqueCode();

        await using var context = fixture.CreateContext();
        context.FieldsOfStudy.Add(new FieldOfStudy { Code = code, Name = "First", Slug = TaxonomyPersistenceTests.UniqueSlug() });
        await context.SaveChangesAsync();

        context.FieldsOfStudy.Add(new FieldOfStudy { Code = code, Name = "Second", Slug = TaxonomyPersistenceTests.UniqueSlug() });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
