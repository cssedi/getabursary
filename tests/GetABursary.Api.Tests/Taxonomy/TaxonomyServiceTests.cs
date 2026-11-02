using GetABursary.Api.Services.Taxonomy;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Repositories.Taxonomy;

namespace GetABursary.Api.Tests.Taxonomy;

public class TaxonomyServiceTests
{
    [Fact]
    public async Task GetFacultiesAsync_UnknownInstitution_ReturnsInstitutionNotFound()
    {
        var repository = new FakeTaxonomyRepository();
        var service = new TaxonomyService(repository);
        var unknownId = Guid.CreateVersion7();

        var result = await service.GetFacultiesAsync(unknownId, CancellationToken.None);

        AssertSingleError(result, "Taxonomy.InstitutionNotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task GetFacultiesAsync_KnownInstitution_ReturnsItsFacultiesOnly()
    {
        var institutionId = Guid.CreateVersion7();
        var otherInstitutionId = Guid.CreateVersion7();
        var commerce = new FacultySummary(Guid.CreateVersion7(), "Commerce", "commerce");
        var law = new FacultySummary(Guid.CreateVersion7(), "Law", "law");
        var elsewhere = new FacultySummary(Guid.CreateVersion7(), "Science", "science");
        var repository = new FakeTaxonomyRepository
        {
            Institutions = [new InstitutionSummary(institutionId, "Uni", "U", "uni", InstitutionType.TraditionalUniversity, Province.Gauteng)],
            Faculties = [law, elsewhere, commerce],
        };
        repository.FacultyIdToInstitutionId[commerce.Id] = institutionId;
        repository.FacultyIdToInstitutionId[law.Id] = institutionId;
        repository.FacultyIdToInstitutionId[elsewhere.Id] = otherInstitutionId;
        var service = new TaxonomyService(repository);

        var result = await service.GetFacultiesAsync(institutionId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Commerce", "Law"], result.Value.Select(f => f.Name));
    }

    [Fact]
    public async Task GetQualificationsByInstitutionAsync_UnknownInstitution_ReturnsInstitutionNotFound()
    {
        var service = new TaxonomyService(new FakeTaxonomyRepository());

        var result = await service.GetQualificationsByInstitutionAsync(Guid.CreateVersion7(), CancellationToken.None);

        AssertSingleError(result, "Taxonomy.InstitutionNotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task GetQualificationsAsync_UnknownFaculty_ReturnsFacultyNotFound()
    {
        var repository = new FakeTaxonomyRepository();
        var service = new TaxonomyService(repository);

        var result = await service.GetQualificationsAsync(Guid.CreateVersion7(), CancellationToken.None);

        AssertSingleError(result, "Taxonomy.FacultyNotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task GetQualificationsAsync_KnownFaculty_SortsFieldCodesAndMapsStudyLevel()
    {
        var facultyId = Guid.CreateVersion7();
        var qualification = new QualificationSummary(
            Guid.CreateVersion7(),
            "Bachelor of Business Science",
            "BBusSc",
            StudyLevel.Undergraduate,
            "bbusc",
            ["15", "04"]);
        var repository = new FakeTaxonomyRepository
        {
            Faculties = [new FacultySummary(facultyId, "Commerce", "commerce")],
            Qualifications = [qualification],
        };
        repository.QualificationIdToFacultyId[qualification.Id] = facultyId;
        var service = new TaxonomyService(repository);

        var result = await service.GetQualificationsAsync(facultyId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value);
        Assert.Equal(StudyLevel.Undergraduate, item.StudyLevel);
        Assert.Equal(["04", "15"], item.FieldCodes);
    }

    [Fact]
    public async Task GetInstitutionsAsync_DefaultCall_RequestsOnlyInstitutionsWithQualifications()
    {
        var repository = new FakeTaxonomyRepository();
        var service = new TaxonomyService(repository);

        await service.GetInstitutionsAsync(includeAll: false, CancellationToken.None);

        Assert.True(repository.LastOnlyWithQualifications);
    }

    [Fact]
    public async Task GetInstitutionsAsync_IncludeAll_DoesNotFilterByQualifications()
    {
        var repository = new FakeTaxonomyRepository();
        var service = new TaxonomyService(repository);

        await service.GetInstitutionsAsync(includeAll: true, CancellationToken.None);

        Assert.False(repository.LastOnlyWithQualifications);
    }

    [Fact]
    public async Task GetInstitutionsAsync_DefaultCall_ExcludesInstitutionsWithoutQualifications()
    {
        var withQualifications = new InstitutionSummary(Guid.CreateVersion7(), "Wits", "Wits", "wits", InstitutionType.TraditionalUniversity, Province.Gauteng);
        var withoutQualifications = new InstitutionSummary(Guid.CreateVersion7(), "Abc", "ABC", "abc", InstitutionType.Private, Province.Gauteng);
        var repository = new FakeTaxonomyRepository { Institutions = [withQualifications, withoutQualifications] };
        repository.InstitutionIdsWithQualifications.Add(withQualifications.Id);
        var service = new TaxonomyService(repository);

        var result = await service.GetInstitutionsAsync(includeAll: false, CancellationToken.None);

        var item = Assert.Single(result.Value);
        Assert.Equal(withQualifications.Id, item.Id);
    }

    [Fact]
    public async Task GetFieldsOfStudyAsync_SecondOrderFields_NestUnderTheirFirstOrderParent()
    {
        var engineering = new FieldOfStudySummary(Guid.CreateVersion7(), "08", "Engineering", "engineering", null);
        var chemical = new FieldOfStudySummary(Guid.CreateVersion7(), "0801", "Chemical Engineering", "chemical-engineering", engineering.Id);
        var mathematics = new FieldOfStudySummary(Guid.CreateVersion7(), "15", "Mathematics and Statistics", "mathematics", null);
        var repository = new FakeTaxonomyRepository { FieldsOfStudy = [chemical, mathematics, engineering] };
        var service = new TaxonomyService(repository);

        var result = await service.GetFieldsOfStudyAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["08", "15"], result.Value.Select(f => f.Code));
        var engineeringResponse = result.Value[0];
        Assert.Null(engineeringResponse.ParentCode);
        var child = Assert.Single(engineeringResponse.Children);
        Assert.Equal("0801", child.Code);
        Assert.Equal("08", child.ParentCode);
        Assert.Empty(child.Children);
        Assert.Empty(result.Value[1].Children);
    }

    private static void AssertSingleError<T>(Result<T> result, string code, ErrorType type)
    {
        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(code, error.Code);
        Assert.Equal(type, error.Type);
    }
}
