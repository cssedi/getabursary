using GetABursary.Core.Data;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Reports;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Repositories.Admin;

public class AdminRepository(GetABursaryDbContext db) : IAdminRepository
{
    private const double MinSimilarity = 0.3;

    public async Task<(IReadOnlyList<ReviewItem> Items, int Total)> ListOpenReviewItemsAsync(
        ReviewItemType? type, int skip, int take, CancellationToken ct)
    {
        var query = db.ReviewItems.AsNoTracking()
            .Where(r => r.Status == ReviewStatus.Open && (type == null || r.Type == type));
        var total = await query.CountAsync(ct);
        var items = await query
            .Include(r => r.Bursary).ThenInclude(b => b!.Funder)
            .Include(r => r.ListingReport)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Skip(skip).Take(take)
            .ToListAsync(ct);
        return (items, total);
    }

    public Task<ReviewItem?> GetReviewItemAsync(Guid id, CancellationToken ct) =>
        db.ReviewItems
            .Include(r => r.Bursary).ThenInclude(b => b!.Funder)
            .Include(r => r.ListingReport)
            .SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task<(IReadOnlyList<Bursary> Items, int Total)> SearchBursariesAsync(
        string? normalizedQuery, bool? published, int skip, int take, CancellationToken ct)
    {
        var query = db.Bursaries.AsNoTracking().Where(b => published == null || b.IsPublished == published);
        var ordered = query.OrderBy(b => b.Name);
        if (!string.IsNullOrEmpty(normalizedQuery))
        {
            query = query.Where(b => b.NormalizedName.Contains(normalizedQuery)
                || EF.Functions.TrigramsSimilarity(b.NormalizedName, normalizedQuery) >= MinSimilarity);
            ordered = query.OrderByDescending(b => EF.Functions.TrigramsSimilarity(b.NormalizedName, normalizedQuery))
                .ThenBy(b => b.Name);
        }

        var total = await query.CountAsync(ct);
        var items = await ordered.Include(b => b.Funder).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public Task<Bursary?> GetBursaryAsync(Guid id, CancellationToken ct) =>
        db.Bursaries.AsNoTracking()
            .Include(b => b.Funder)
            .Include(b => b.Fields)
            .SingleOrDefaultAsync(b => b.Id == id, ct);

    public Task<Bursary?> GetBursaryForUpdateAsync(Guid id, CancellationToken ct) =>
        db.Bursaries
            .Include(b => b.Funder)
            .Include(b => b.Fields)
            .SingleOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<string>> GetFieldCodesAsync(IEnumerable<Guid> fieldIds, CancellationToken ct)
    {
        var ids = fieldIds.ToArray();
        return await db.FieldsOfStudy.AsNoTracking().Where(f => ids.Contains(f.Id)).Select(f => f.Code).OrderBy(c => c).ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
