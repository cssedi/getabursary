using GetABursary.Core.Data;
using GetABursary.Core.Domain.Ingestion;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Repositories.Ingestion;

public class RawListingRepository(GetABursaryDbContext db) : IRawListingRepository
{
    public Task<RawListing?> GetBySourceAndUrlAsync(Guid sourceId, string url, CancellationToken ct) =>
        db.RawListings.SingleOrDefaultAsync(r => r.SourceId == sourceId && r.Url == url, ct);

    public void Add(RawListing listing) => db.RawListings.Add(listing);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
