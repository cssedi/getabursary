using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Repositories.Bursaries;
using Microsoft.AspNetCore.Mvc;

namespace GetABursary.Api.DTOs.Bursaries;

/// <summary>Query string of <c>GET /api/v1/bursaries/search</c>.</summary>
public sealed record BursarySearchRequest
{
    public Guid? QualificationId { get; init; }

    /// <summary>Repeatable: <c>fieldIds=...&amp;fieldIds=...</c>.</summary>
    public IReadOnlyList<Guid>? FieldIds { get; init; }

    public Guid? InstitutionId { get; init; }

    public StudyLevel? StudyLevel { get; init; }

    /// <summary>The student's average, 0 to 100.</summary>
    public decimal? Average { get; init; }

    public DateOnly? ClosingFrom { get; init; }

    public DateOnly? ClosingTo { get; init; }

    public bool IncludeClosed { get; init; }

    public BursarySort Sort { get; init; } = BursarySort.BestMatch;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
