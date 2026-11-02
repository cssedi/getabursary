using GetABursary.Core.Common;

namespace GetABursary.Api.Services.Taxonomy;

public static class TaxonomyErrors
{
    public static Error InstitutionNotFound(Guid institutionId) =>
        Error.NotFound("Taxonomy.InstitutionNotFound", $"Institution {institutionId} was not found");

    public static Error FacultyNotFound(Guid facultyId) =>
        Error.NotFound("Taxonomy.FacultyNotFound", $"Faculty {facultyId} was not found");
}
