using GetABursary.Core.Domain.Reports;

namespace GetABursary.Core.Repositories.Reports;

/// <summary>Data access for listing reports and their review items. Contains no business rules.</summary>
public interface IListingReportRepository
{
    /// <summary>True when a bursary with this id exists.</summary>
    Task<bool> BursaryExistsAsync(Guid bursaryId, CancellationToken ct);

    /// <summary>Tracks a new report. Nothing is written until <see cref="SaveChangesAsync"/>.</summary>
    void AddReport(ListingReport report);

    /// <summary>Tracks a new review item. Nothing is written until <see cref="SaveChangesAsync"/>.</summary>
    void AddReviewItem(ReviewItem item);

    /// <summary>Writes all tracked changes in one transaction. Called once per use case by the service.</summary>
    Task SaveChangesAsync(CancellationToken ct);
}
