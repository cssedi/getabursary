namespace GetABursary.Api.DTOs.Common;

/// <summary>A page of results plus the total number of matches across all pages.</summary>
public sealed record PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }
}
