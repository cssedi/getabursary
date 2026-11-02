using GetABursary.Core.Data;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Reports;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Repositories.Reports;

public class ReviewItemRepository(GetABursaryDbContext db) : IReviewItemRepository
{
    public Task<bool> HasOpenAsync(Guid bursaryId, ReviewItemType type, CancellationToken ct) =>
        db.ReviewItems.AsNoTracking()
            .AnyAsync(r => r.BursaryId == bursaryId && r.Type == type && r.Status == ReviewStatus.Open, ct);

    public void Add(ReviewItem item) => db.ReviewItems.Add(item);
}
