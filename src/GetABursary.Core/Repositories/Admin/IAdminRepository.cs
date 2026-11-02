using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Reports;

namespace GetABursary.Core.Repositories.Admin;

public interface IAdminRepository
{
    /// <summary>Open review items, newest first, with bursary, funder and report loaded (read-only).</summary>
    Task<(IReadOnlyList<ReviewItem> Items, int Total)> ListOpenReviewItemsAsync(
        ReviewItemType? type, int skip, int take, CancellationToken ct);

    /// <summary>A tracked review item with its bursary and report.</summary>
    Task<ReviewItem?> GetReviewItemAsync(Guid id, CancellationToken ct);

    /// <summary>All bursaries (published or not) matching a trigram name search, best match first (read-only).</summary>
    Task<(IReadOnlyList<Bursary> Items, int Total)> SearchBursariesAsync(
        string? normalizedQuery, bool? published, int skip, int take, CancellationToken ct);

    /// <summary>A bursary with funder and field links (read-only).</summary>
    Task<Bursary?> GetBursaryAsync(Guid id, CancellationToken ct);

    /// <summary>A tracked bursary with funder and field links.</summary>
    Task<Bursary?> GetBursaryForUpdateAsync(Guid id, CancellationToken ct);

    /// <summary>Field-of-study codes for the given ids.</summary>
    Task<IReadOnlyList<string>> GetFieldCodesAsync(IEnumerable<Guid> fieldIds, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
