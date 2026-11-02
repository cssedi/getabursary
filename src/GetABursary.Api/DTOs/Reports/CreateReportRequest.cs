using System.ComponentModel.DataAnnotations;
using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Api.DTOs.Reports;

public sealed record CreateReportRequest
{
    /// <summary>Nullable so that a missing reason is a validation error rather than silently defaulting.</summary>
    [Required]
    [EnumDataType(typeof(ReportReason))]
    public ReportReason? Reason { get; init; }

    [MaxLength(1000)]
    public string? Comment { get; init; }

    [EmailAddress]
    [MaxLength(254)]
    public string? ContactEmail { get; init; }
}
