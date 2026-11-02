using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Taxonomy;
using GetABursary.Core.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Tests.Data.Taxonomy;

[Collection("Postgres")]
public class TaxonomyPersistenceTests(PostgresFixture fixture)
{
    private static readonly string[] TaxonomyTables =
    [
        "field_of_study", "institution", "faculty", "qualification", "qualification_field",
    ];

    // Constant table names, inlined so EF does not expand an array parameter into a list.
    private const string TaxonomyTableList =
        "'field_of_study', 'institution', 'faculty', 'qualification', 'qualification_field'";

    [Fact]
    public async Task Institution_Graph_Saves_And_Reloads_With_Include()
    {
        var parentField = new FieldOfStudy { Code = UniqueCode(), Name = "Engineering", Slug = UniqueSlug() };
        var field = new FieldOfStudy
        {
            Code = UniqueCode(),
            Name = "Electrical Engineering",
            Slug = UniqueSlug(),
            Parent = parentField,
        };
        var secondField = new FieldOfStudy { Code = UniqueCode(), Name = "Computer Science", Slug = UniqueSlug() };

        var institution = new Institution
        {
            Name = "University of Example",
            ShortName = "UEx",
            Slug = UniqueSlug(),
            Type = InstitutionType.TraditionalUniversity,
            Province = Province.WesternCape,
            WebsiteUrl = "https://example.test",
        };
        var faculty = new Faculty { Name = "Engineering & the Built Environment", Slug = "eng" };
        var qualification = new Qualification
        {
            Name = "BSc (Eng) Electrical Engineering",
            Abbreviation = "BSc (Eng)",
            StudyLevel = StudyLevel.Undergraduate,
            NqfLevel = 8,
            DurationYears = 4,
            Slug = "bsc-eng-electrical",
        };
        qualification.QualificationFields.Add(new QualificationField { Qualification = qualification, FieldOfStudy = field });
        qualification.QualificationFields.Add(new QualificationField { Qualification = qualification, FieldOfStudy = secondField });
        faculty.Qualifications.Add(qualification);
        institution.Faculties.Add(faculty);

        await using (var writer = fixture.CreateContext())
        {
            writer.Institutions.Add(institution);
            await writer.SaveChangesAsync();
        }

        await using var reader = fixture.CreateContext();
        var loaded = await reader.Institutions
            .AsNoTracking()
            .Include(i => i.Faculties)
                .ThenInclude(f => f.Qualifications)
                    .ThenInclude(q => q.QualificationFields)
                        .ThenInclude(qf => qf.FieldOfStudy)
            .SingleAsync(i => i.Id == institution.Id);

        Assert.Equal(InstitutionType.TraditionalUniversity, loaded.Type);
        Assert.Equal(Province.WesternCape, loaded.Province);

        var loadedFaculty = Assert.Single(loaded.Faculties);
        Assert.Equal(faculty.Id, loadedFaculty.Id);

        var loadedQualification = Assert.Single(loadedFaculty.Qualifications);
        Assert.Equal(StudyLevel.Undergraduate, loadedQualification.StudyLevel);
        Assert.Equal(8, loadedQualification.NqfLevel);

        var fieldNames = loadedQualification.QualificationFields
            .Select(qf => qf.FieldOfStudy.Name)
            .OrderBy(n => n)
            .ToList();
        Assert.Equal(["Computer Science", "Electrical Engineering"], fieldNames);
    }

    [Fact]
    public async Task Taxonomy_Tables_And_Columns_Use_Snake_Case()
    {
        await using var context = fixture.CreateContext();

        var tables = await context.Database
            .SqlQueryRaw<string>(
                $"""
                SELECT table_name AS "Value" FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name IN ({TaxonomyTableList})
                """)
            .ToListAsync();
        Assert.Equal(TaxonomyTables.Order(), tables.Order());

        var joinColumn = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT column_name AS "Value" FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'qualification_field' AND column_name = 'field_of_study_id'
                """)
            .ToListAsync();
        Assert.Single(joinColumn);

        var mixedCaseColumns = await context.Database
            .SqlQueryRaw<string>(
                $"""
                SELECT column_name AS "Value" FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name IN ({TaxonomyTableList}) AND column_name <> lower(column_name)
                """)
            .ToListAsync();
        Assert.Empty(mixedCaseColumns);
    }

    internal static string UniqueCode() => "T" + Guid.NewGuid().ToString("N")[..9];

    internal static string UniqueSlug() => "s" + Guid.NewGuid().ToString("N");
}
