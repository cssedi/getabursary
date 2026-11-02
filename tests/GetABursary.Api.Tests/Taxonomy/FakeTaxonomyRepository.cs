using GetABursary.Core.Repositories.Taxonomy;

namespace GetABursary.Api.Tests.Taxonomy;

/// <summary>In-memory stand-in for <see cref="ITaxonomyRepository"/>. Filtering mirrors the SQL the real repository runs.</summary>
public sealed class FakeTaxonomyRepository : ITaxonomyRepository
{
    public List<InstitutionSummary> Institutions { get; set; } = [];

    /// <summary>Institutions that have at least one qualification (the default picker filter).</summary>
    public HashSet<Guid> InstitutionIdsWithQualifications { get; } = [];

    public List<FacultySummary> Faculties { get; set; } = [];

    public Dictionary<Guid, Guid> FacultyIdToInstitutionId { get; } = [];

    public List<QualificationSummary> Qualifications { get; set; } = [];

    public Dictionary<Guid, Guid> QualificationIdToFacultyId { get; } = [];

    public List<FieldOfStudySummary> FieldsOfStudy { get; set; } = [];

    /// <summary>The <c>onlyWithQualifications</c> value from the most recent <see cref="GetInstitutionsAsync"/> call.</summary>
    public bool? LastOnlyWithQualifications { get; private set; }

    public Task<bool> InstitutionExistsAsync(Guid institutionId, CancellationToken ct) =>
        Task.FromResult(Institutions.Any(i => i.Id == institutionId));

    public Task<IReadOnlyList<InstitutionSummary>> GetInstitutionsAsync(bool onlyWithQualifications, CancellationToken ct)
    {
        LastOnlyWithQualifications = onlyWithQualifications;

        IReadOnlyList<InstitutionSummary> result = Institutions
            .Where(i => !onlyWithQualifications || InstitutionIdsWithQualifications.Contains(i.Id))
            .OrderBy(i => i.Name, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<bool> FacultyExistsAsync(Guid facultyId, CancellationToken ct) =>
        Task.FromResult(Faculties.Any(f => f.Id == facultyId));

    public Task<IReadOnlyList<FacultySummary>> GetFacultiesAsync(Guid institutionId, CancellationToken ct)
    {
        IReadOnlyList<FacultySummary> result = Faculties
            .Where(f => FacultyIdToInstitutionId.TryGetValue(f.Id, out var owner) && owner == institutionId)
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<QualificationSummary>> GetQualificationsAsync(Guid facultyId, CancellationToken ct)
    {
        IReadOnlyList<QualificationSummary> result = Qualifications
            .Where(q => QualificationIdToFacultyId.TryGetValue(q.Id, out var owner) && owner == facultyId)
            .OrderBy(q => q.Name, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<FieldOfStudySummary>> GetFieldsOfStudyAsync(CancellationToken ct)
    {
        IReadOnlyList<FieldOfStudySummary> result = FieldsOfStudy
            .OrderBy(f => f.Code, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(result);
    }
}
