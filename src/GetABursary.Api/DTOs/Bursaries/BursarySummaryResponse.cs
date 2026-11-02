namespace GetABursary.Api.DTOs.Bursaries;

/// <summary>A bursary in search results. Enum-like values are serialised as their names.</summary>
public sealed record BursarySummaryResponse
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public string? FunderName { get; init; }

    public DateOnly? ClosingDate { get; init; }

    public required bool IsClosingSoon { get; init; }

    public required string Status { get; init; }

    public required IReadOnlyList<string> StudyLevels { get; init; }

    public decimal? MinAverage { get; init; }

    public required IReadOnlyList<string> Coverage { get; init; }

    public required IReadOnlyList<BursarySourceResponse> Sources { get; init; }

    public required decimal MatchScore { get; init; }
}

/// <summary>An aggregator page the bursary was found on; <c>IconUrl</c> drives the source icon.</summary>
public sealed record BursarySourceResponse
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public string? IconUrl { get; init; }

    public required string Url { get; init; }
}
