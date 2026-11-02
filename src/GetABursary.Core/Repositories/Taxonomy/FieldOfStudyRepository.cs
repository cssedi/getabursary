using GetABursary.Core.Data;
using GetABursary.Core.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Repositories.Taxonomy;

public class FieldOfStudyRepository(GetABursaryDbContext db) : IFieldOfStudyRepository
{
    public async Task<IReadOnlyList<FieldOfStudy>> ListAllAsync(CancellationToken ct) =>
        await db.FieldsOfStudy.AsNoTracking().OrderBy(f => f.Code).ToListAsync(ct);
}
