using GetABursary.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Repositories.Taxonomy;

public sealed class QualificationResolutionRepository(GetABursaryDbContext db) : IQualificationResolutionRepository
{
    public async Task<QualificationResolution?> ResolveAsync(Guid qualificationId, CancellationToken ct)
    {
        var row = await db.Qualifications.AsNoTracking()
            .Where(q => q.Id == qualificationId)
            .Select(q => new
            {
                q.Id,
                q.Faculty.InstitutionId,
                Fields = q.QualificationFields
                    .Select(qf => new { qf.FieldOfStudyId, qf.FieldOfStudy.ParentId })
                    .ToList(),
            })
            .SingleOrDefaultAsync(ct);

        if (row is null)
        {
            return null;
        }

        var ids = row.Fields
            .SelectMany(f => f.ParentId is { } parent ? new[] { f.FieldOfStudyId, parent } : [f.FieldOfStudyId])
            .Distinct()
            .ToList();

        return new QualificationResolution(row.Id, row.InstitutionId, ids);
    }
}
