using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Core.Repositories.Bursaries;

/// <summary>How search results are ordered. Closed bursaries (when included) always sort last.</summary>
public enum BursarySort
{
    BestMatch = 0,
    ClosingSoonest = 1,
    Newest = 2,
}

/// <summary>
/// Resolved search input (specs/02-data-model.md section 5). <see cref="FieldIds"/> already holds the full target
/// field set (a qualification's fields plus their CESM parents).
/// </summary>
public sealed record BursarySearchCriteria
{
    public Guid? QualificationId { get; init; }

    public IReadOnlyList<Guid> FieldIds { get; init; } = [];

    public Guid? InstitutionId { get; init; }

    public StudyLevel? StudyLevel { get; init; }

    public decimal? Average { get; init; }

    public DateOnly? ClosingFrom { get; init; }

    public DateOnly? ClosingTo { get; init; }

    public bool IncludeClosed { get; init; }

    public BursarySort Sort { get; init; } = BursarySort.BestMatch;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
