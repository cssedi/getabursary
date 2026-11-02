using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common;
using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Core.Domain.Reports;

/// <summary>An anonymous user's report that a bursary listing is inaccurate.</summary>
public class ListingReport : Entity
{
    public Guid BursaryId { get; set; }

    public Bursary Bursary { get; set; } = null!;

    public ReportReason Reason { get; set; }

    public required string Comment { get; set; }

    public string? ContactEmail { get; set; }

    /// <summary>Salted SHA-256 of the client IP. Raw IP addresses are never stored (POPIA).</summary>
    public required string IpHash { get; set; }

    public ReviewStatus Status { get; set; } = ReviewStatus.Open;
}
