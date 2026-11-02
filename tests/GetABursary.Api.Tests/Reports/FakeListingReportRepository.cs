using GetABursary.Core.Domain.Reports;
using GetABursary.Core.Repositories.Reports;

namespace GetABursary.Api.Tests.Reports;

/// <summary>In-memory stand-in for <see cref="IListingReportRepository"/> that records what the service asked to save.</summary>
public sealed class FakeListingReportRepository : IListingReportRepository
{
    public HashSet<Guid> KnownBursaryIds { get; set; } = [];

    public List<ListingReport> Reports { get; } = [];

    public List<ReviewItem> ReviewItems { get; } = [];

    public int SaveChangesCalls { get; private set; }

    public Task<bool> BursaryExistsAsync(Guid bursaryId, CancellationToken ct) =>
        Task.FromResult(KnownBursaryIds.Contains(bursaryId));

    public void AddReport(ListingReport report) => Reports.Add(report);

    public void AddReviewItem(ReviewItem item) => ReviewItems.Add(item);

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveChangesCalls++;
        return Task.CompletedTask;
    }
}
