using GetABursary.Api.DTOs.Taxonomy;
using GetABursary.Core.Common;

namespace GetABursary.Api.Services.Taxonomy;

public interface ITaxonomyService
{
    /// <summary>Institutions sorted by name. Unless <paramref name="includeAll"/> is true, only those with at least one qualification.</summary>
    Task<Result<IReadOnlyList<InstitutionResponse>>> GetInstitutionsAsync(bool includeAll, CancellationToken ct);

    /// <summary>Faculties of an institution. Fails with <c>Taxonomy.InstitutionNotFound</c> for an unknown id.</summary>
    Task<Result<IReadOnlyList<FacultyResponse>>> GetFacultiesAsync(Guid institutionId, CancellationToken ct);

    /// <summary>Qualifications of a faculty. Fails with <c>Taxonomy.FacultyNotFound</c> for an unknown id.</summary>
    Task<Result<IReadOnlyList<QualificationResponse>>> GetQualificationsAsync(Guid facultyId, CancellationToken ct);

    /// <summary>Fields of study as a two-level tree (first order with nested second order).</summary>
    Task<Result<IReadOnlyList<FieldOfStudyResponse>>> GetFieldsOfStudyAsync(CancellationToken ct);
}
