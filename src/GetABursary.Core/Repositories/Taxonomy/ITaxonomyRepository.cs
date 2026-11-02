using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Core.Repositories.Taxonomy;

/// <summary>Read-only data access for the taxonomy (institutions, faculties, qualifications, fields of study). Contains no business rules.</summary>
public interface ITaxonomyRepository
{
    /// <summary>True when an institution with this id exists.</summary>
    Task<bool> InstitutionExistsAsync(Guid institutionId, CancellationToken ct);

    /// <summary>Institutions sorted by name. When <paramref name="onlyWithQualifications"/> is true, only those with at least one qualification.</summary>
    Task<IReadOnlyList<InstitutionSummary>> GetInstitutionsAsync(bool onlyWithQualifications, CancellationToken ct);

    /// <summary>True when a faculty with this id exists.</summary>
    Task<bool> FacultyExistsAsync(Guid facultyId, CancellationToken ct);

    /// <summary>Faculties of one institution, sorted by name. Empty when the institution has none or does not exist.</summary>
    Task<IReadOnlyList<FacultySummary>> GetFacultiesAsync(Guid institutionId, CancellationToken ct);

    /// <summary>Qualifications of one faculty, sorted by name, with the CESM codes of the fields they cover.</summary>
    Task<IReadOnlyList<QualificationSummary>> GetQualificationsAsync(Guid facultyId, CancellationToken ct);

    /// <summary>All fields of study (first and second order), sorted by code. The service arranges them into a tree.</summary>
    Task<IReadOnlyList<FieldOfStudySummary>> GetFieldsOfStudyAsync(CancellationToken ct);
}

/// <summary>Projection of an institution for the picker.</summary>
public sealed record InstitutionSummary(
    Guid Id,
    string Name,
    string ShortName,
    string Slug,
    InstitutionType Type,
    Province Province);

/// <summary>Projection of a faculty.</summary>
public sealed record FacultySummary(Guid Id, string Name, string Slug);

/// <summary>Projection of a qualification and the CESM codes of the fields it covers.</summary>
public sealed record QualificationSummary(
    Guid Id,
    string Name,
    string Abbreviation,
    StudyLevel StudyLevel,
    string Slug,
    IReadOnlyList<string> FieldCodes);

/// <summary>Projection of a CESM field of study. <c>ParentId</c> is null for first-order fields.</summary>
public sealed record FieldOfStudySummary(Guid Id, string Code, string Name, string Slug, Guid? ParentId);
