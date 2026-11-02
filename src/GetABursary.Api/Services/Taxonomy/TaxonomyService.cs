using GetABursary.Api.DTOs.Taxonomy;
using GetABursary.Core.Common;
using GetABursary.Core.Repositories.Taxonomy;

namespace GetABursary.Api.Services.Taxonomy;

public sealed class TaxonomyService(ITaxonomyRepository repository) : ITaxonomyService
{
    public async Task<Result<IReadOnlyList<InstitutionResponse>>> GetInstitutionsAsync(bool includeAll, CancellationToken ct)
    {
        var institutions = await repository.GetInstitutionsAsync(onlyWithQualifications: !includeAll, ct);

        return institutions
            .Select(i => new InstitutionResponse
            {
                Id = i.Id,
                Name = i.Name,
                ShortName = i.ShortName,
                Slug = i.Slug,
                Type = i.Type,
                Province = i.Province,
            })
            .ToList();
    }

    public async Task<Result<IReadOnlyList<FacultyResponse>>> GetFacultiesAsync(Guid institutionId, CancellationToken ct)
    {
        if (!await repository.InstitutionExistsAsync(institutionId, ct))
        {
            return TaxonomyErrors.InstitutionNotFound(institutionId);
        }

        var faculties = await repository.GetFacultiesAsync(institutionId, ct);

        return faculties
            .Select(f => new FacultyResponse { Id = f.Id, Name = f.Name, Slug = f.Slug })
            .ToList();
    }

    public async Task<Result<IReadOnlyList<QualificationResponse>>> GetQualificationsAsync(Guid facultyId, CancellationToken ct)
    {
        if (!await repository.FacultyExistsAsync(facultyId, ct))
        {
            return TaxonomyErrors.FacultyNotFound(facultyId);
        }

        var qualifications = await repository.GetQualificationsAsync(facultyId, ct);

        return qualifications.Select(ToResponse).ToList();
    }

    public async Task<Result<IReadOnlyList<FacultyQualificationsResponse>>> GetQualificationsByInstitutionAsync(Guid institutionId, CancellationToken ct)
    {
        var faculties = await repository.GetFacultiesWithQualificationsAsync(institutionId, ct);

        if (faculties is null)
        {
            return TaxonomyErrors.InstitutionNotFound(institutionId);
        }

        return faculties
            .Select(f => new FacultyQualificationsResponse
            {
                Id = f.Id,
                Name = f.Name,
                Slug = f.Slug,
                Qualifications = f.Qualifications.Select(ToResponse).ToList(),
            })
            .ToList();
    }

    private static QualificationResponse ToResponse(QualificationSummary q) =>
        new()
        {
            Id = q.Id,
            Name = q.Name,
            Abbreviation = q.Abbreviation,
            StudyLevel = q.StudyLevel,
            Slug = q.Slug,
            FieldCodes = q.FieldCodes.Order(StringComparer.Ordinal).ToList(),
        };

    public async Task<Result<IReadOnlyList<FieldOfStudyResponse>>> GetFieldsOfStudyAsync(CancellationToken ct)
    {
        var fields = await repository.GetFieldsOfStudyAsync(ct);

        var childrenByParent = fields
            .Where(f => f.ParentId is not null)
            .GroupBy(f => f.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        return fields
            .Where(f => f.ParentId is null)
            .Select(parent => ToResponse(parent, parentCode: null, childrenByParent))
            .ToList();
    }

    private static FieldOfStudyResponse ToResponse(
        FieldOfStudySummary field,
        string? parentCode,
        IReadOnlyDictionary<Guid, List<FieldOfStudySummary>> childrenByParent) =>
        new()
        {
            Id = field.Id,
            Code = field.Code,
            Name = field.Name,
            Slug = field.Slug,
            ParentCode = parentCode,
            Children = childrenByParent.TryGetValue(field.Id, out var children)
                ? children.Select(child => ToResponse(child, field.Code, childrenByParent)).ToList()
                : [],
        };
}
