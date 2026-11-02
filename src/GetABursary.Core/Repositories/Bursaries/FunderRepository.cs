using GetABursary.Core.Data;
using GetABursary.Core.Domain.Bursaries;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Repositories.Bursaries;

public class FunderRepository(GetABursaryDbContext db) : IFunderRepository
{
    public Task<Funder?> GetByNormalizedNameAsync(string normalizedName, CancellationToken ct) =>
        db.Funders.SingleOrDefaultAsync(f => f.NormalizedName == normalizedName, ct);

    public void Add(Funder funder) => db.Funders.Add(funder);
}
