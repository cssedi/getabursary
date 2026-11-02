using GetABursary.Core.Data;
using GetABursary.Core.Domain.Reports;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Repositories.Reports;

public sealed class ListingReportRepository(GetABursaryDbContext db) : IListingReportRepository
{
    public Task<bool> BursaryExistsAsync(Guid bursaryId, CancellationToken ct) =>
        db.Bursaries.AsNoTracking().AnyAsync(b => b.Id == bursaryId, ct);

    public void AddReport(ListingReport report) => db.ListingReports.Add(report);

    public void AddReviewItem(ReviewItem item) => db.ReviewItems.Add(item);

    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
