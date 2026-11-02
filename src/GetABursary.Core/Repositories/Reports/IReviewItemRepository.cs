using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Reports;

namespace GetABursary.Core.Repositories.Reports;

public interface IReviewItemRepository
{
    /// <summary>True when an open review item of this type already exists for the bursary.</summary>
    Task<bool> HasOpenAsync(Guid bursaryId, ReviewItemType type, CancellationToken ct);

    void Add(ReviewItem item);
}
