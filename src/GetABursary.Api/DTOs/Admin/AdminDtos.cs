using System.Text.Json;
using System.Text.Json.Serialization;

namespace GetABursary.Api.DTOs.Admin;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewAction
{
    Publish,
    Dismiss,
    MarkClosed,
}

/// <summary>Body of <c>POST /api/v1/admin/review-items/{id}/resolve</c>.</summary>
public sealed record ResolveReviewItemRequest
{
    public ReviewAction Action { get; init; }

    public string? Notes { get; init; }
}

public sealed record ReviewItemResponse
{
    public required Guid Id { get; init; }

    public required string Type { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public Guid? BursaryId { get; init; }

    public string? BursaryName { get; init; }

    public string? FunderName { get; init; }

    public bool? IsPublished { get; init; }

    public string? BursaryStatus { get; init; }

    public DateOnly? ClosingDate { get; init; }

    /// <summary>Why the item was raised (ingestion details such as confidence or the duplicate candidate).</summary>
    public JsonElement? Reason { get; init; }

    public string? ReportReason { get; init; }

    public string? ReportComment { get; init; }
}

public sealed record AdminBursarySummaryResponse
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public string? FunderName { get; init; }

    public required bool IsPublished { get; init; }

    public required string Status { get; init; }

    public DateOnly? ClosingDate { get; init; }
}

public sealed record AdminBursaryResponse
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public string? FunderName { get; init; }

    public string? Summary { get; init; }

    public DateOnly? OpenDate { get; init; }

    public DateOnly? ClosingDate { get; init; }

    public required IReadOnlyList<string> StudyLevels { get; init; }

    public decimal? MinAverage { get; init; }

    public required IReadOnlyList<string> Coverage { get; init; }

    public required IReadOnlyList<string> FieldCodes { get; init; }

    public string? ApplyUrl { get; init; }

    public required bool IsPublished { get; init; }

    public required string Status { get; init; }

    public required IReadOnlyList<string> LockedFields { get; init; }
}
