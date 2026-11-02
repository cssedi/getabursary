using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Core.Repositories.Bursaries;

/// <summary>One search hit as projected from the database.</summary>
public sealed record BursarySearchRow(
    Guid Id,
    string Slug,
    string Name,
    string? FunderName,
    DateOnly? ClosingDate,
    BursaryStatus Status,
    IReadOnlyList<StudyLevel> StudyLevels,
    decimal? MinAverage,
    IReadOnlyList<CoverageItem> Coverage,
    IReadOnlyList<BursarySearchSource> Sources,
    decimal MatchScore);

/// <summary>An aggregator page a bursary was found on.</summary>
public sealed record BursarySearchSource(string Key, string Name, string? IconUrl, string Url);

/// <summary>A page of hits plus the total number of matches across all pages.</summary>
public sealed record BursarySearchPage(IReadOnlyList<BursarySearchRow> Items, int TotalCount);
