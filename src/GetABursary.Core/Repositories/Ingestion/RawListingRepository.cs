using GetABursary.Core.Data;
using GetABursary.Core.Domain.Ingestion;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Repositories.Ingestion;

public class RawListingRepository(GetABursaryDbContext db) : IRawListingRepository
{
    public Task<RawListing?> GetBySourceAndUrlAsync(Guid sourceId, string url, CancellationToken ct) =>
        db.RawListings.SingleOrDefaultAsync(r => r.SourceId == sourceId && r.Url == url, ct);

    public async Task<RawListing?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await db.RawListings.FindAsync([id], ct);

    public void Add(RawListing listing) => db.RawListings.Add(listing);

    public async Task MarkSeenAsync(Guid rawListingId, DateTimeOffset now, CancellationToken ct)
    {
        var listing = await db.RawListings.AsNoTracking()
            .Where(r => r.Id == rawListingId)
            .Select(r => new { r.SourceId, r.Url, r.BursaryId })
            .SingleOrDefaultAsync(ct);
        if (listing is null)
        {
            return;
        }

        await db.RawListings
            .Where(r => r.Id == rawListingId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.MissedRuns, 0), ct);

        if (listing.BursaryId is not { } bursaryId)
        {
            return;
        }

        // The bursary link stores the URL as Uri.ToString(), the snapshot as Uri.AbsoluteUri; match either form.
        var urls = new[] { listing.Url, new Uri(listing.Url).ToString() };
        await db.BursarySources
            .Where(s => s.BursaryId == bursaryId && s.SourceId == listing.SourceId && urls.Contains(s.SourceUrl))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSeenAt, now), ct);
    }

    public async Task<IReadOnlyList<MissedListing>> IncrementMissedRunsAsync(
        Guid sourceId, IReadOnlyCollection<Guid> seenIds, int threshold, CancellationToken ct)
    {
        var seen = seenIds.ToArray();
        await db.RawListings
            .Where(r => r.SourceId == sourceId && !seen.Contains(r.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.MissedRuns, r => r.MissedRuns + 1), ct);

        return await db.RawListings.AsNoTracking()
            .Where(r => r.SourceId == sourceId && !seen.Contains(r.Id) && r.MissedRuns == threshold && r.BursaryId != null)
            .Select(r => new MissedListing(r.Id, r.BursaryId!.Value, r.Url))
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
