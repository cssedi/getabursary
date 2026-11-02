using GetABursary.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Repositories.Taxonomy;

public sealed class TaxonomyRepository(GetABursaryDbContext db) : ITaxonomyRepository
{
    public Task<bool> InstitutionExistsAsync(Guid institutionId, CancellationToken ct) =>
        db.Institutions.AsNoTracking().AnyAsync(i => i.Id == institutionId, ct);

    public async Task<IReadOnlyList<InstitutionSummary>> GetInstitutionsAsync(bool onlyWithQualifications, CancellationToken ct)
    {
        var query = db.Institutions.AsNoTracking();

        if (onlyWithQualifications)
        {
            query = query.Where(i => i.Faculties.Any(f => f.Qualifications.Count > 0));
        }

        return await query
            .OrderBy(i => i.Name)
            .Select(i => new InstitutionSummary(i.Id, i.Name, i.ShortName, i.Slug, i.Type, i.Province))
            .ToListAsync(ct);
    }

    public Task<bool> FacultyExistsAsync(Guid facultyId, CancellationToken ct) =>
        db.Faculties.AsNoTracking().AnyAsync(f => f.Id == facultyId, ct);

    public async Task<IReadOnlyList<FacultySummary>> GetFacultiesAsync(Guid institutionId, CancellationToken ct) =>
        await db.Faculties.AsNoTracking()
            .Where(f => f.InstitutionId == institutionId)
            .OrderBy(f => f.Name)
            .Select(f => new FacultySummary(f.Id, f.Name, f.Slug))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<QualificationSummary>> GetQualificationsAsync(Guid facultyId, CancellationToken ct) =>
        await db.Qualifications.AsNoTracking()
            .Where(q => q.FacultyId == facultyId)
            .OrderBy(q => q.Name)
            .Select(q => new QualificationSummary(
                q.Id,
                q.Name,
                q.Abbreviation,
                q.StudyLevel,
                q.Slug,
                q.QualificationFields.Select(qf => qf.FieldOfStudy.Code).OrderBy(code => code).ToList()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FieldOfStudySummary>> GetFieldsOfStudyAsync(CancellationToken ct) =>
        await db.FieldsOfStudy.AsNoTracking()
            .OrderBy(f => f.Code)
            .Select(f => new FieldOfStudySummary(f.Id, f.Code, f.Name, f.Slug, f.ParentId))
            .ToListAsync(ct);
}
