using System.Text.Json;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;

namespace GetABursary.Core.Domain.Reports;

/// <summary>An item in the admin review queue.</summary>
public class ReviewItem : Entity
{
    public ReviewItemType Type { get; set; }

    public ReviewStatus Status { get; set; } = ReviewStatus.Open;

    public Guid? BursaryId { get; set; }

    public Bursary? Bursary { get; set; }

    public Guid? RawListingId { get; set; }

    public RawListing? RawListing { get; set; }

    public Guid? ListingReportId { get; set; }

    public ListingReport? ListingReport { get; set; }

    public JsonDocument? Details { get; set; }

    /// <summary>Identity user id. No foreign key: the Identity tables arrive in a later task.</summary>
    public Guid? ResolvedByUserId { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public string Notes { get; set; } = string.Empty;
}
