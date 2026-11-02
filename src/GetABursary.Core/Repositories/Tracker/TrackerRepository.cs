using GetABursary.Core.Data;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GetABursary.Core.Repositories.Tracker;

public sealed class TrackerRepository(GetABursaryDbContext db) : ITrackerRepository
{
    public async Task<IReadOnlyList<TrackerEntry>> ListAsync(Guid userId, CancellationToken ct) =>
        await db.TrackerEntries.AsNoTracking()
            .Where(t => t.UserId == userId)
            .Include(t => t.Bursary).ThenInclude(b => b.Funder)
            .Include(t => t.Bursary).ThenInclude(b => b.Sources).ThenInclude(bs => bs.Source)
            .OrderBy(t => t.Bursary.ClosingDate)
            .ThenBy(t => t.Id)
            .ToListAsync(ct);

    public async Task<Bursary?> GetPublishedBursaryAsync(Guid bursaryId, CancellationToken ct) =>
        await db.Bursaries.AsNoTracking()
            .Include(b => b.Funder)
            .Include(b => b.Sources).ThenInclude(bs => bs.Source)
            .AsSplitQuery()
            .SingleOrDefaultAsync(b => b.Id == bursaryId && b.IsPublished, ct);

    public Task<TrackerEntry?> FindAsync(Guid userId, Guid bursaryId, CancellationToken ct) =>
        db.TrackerEntries.SingleOrDefaultAsync(t => t.UserId == userId && t.BursaryId == bursaryId, ct);

    public void Add(TrackerEntry entry) => db.TrackerEntries.Add(entry);

    public Task DeleteAsync(Guid userId, Guid bursaryId, CancellationToken ct) =>
        db.TrackerEntries.Where(t => t.UserId == userId && t.BursaryId == bursaryId).ExecuteDeleteAsync(ct);

    public async Task<bool> TrySaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            foreach (var e in db.ChangeTracker.Entries<TrackerEntry>().Where(e => e.State == EntityState.Added).ToList())
            {
                e.State = EntityState.Detached;
            }

            return false;
        }
    }
}
