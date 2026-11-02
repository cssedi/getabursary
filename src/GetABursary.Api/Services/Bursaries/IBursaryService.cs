using GetABursary.Api.DTOs.Bursaries;
using GetABursary.Core.Common;

namespace GetABursary.Api.Services.Bursaries;

public interface IBursaryService
{
    /// <summary>
    /// Detail for a published bursary. Unknown or unpublished slugs fail with <c>Bursary.NotFound</c>.
    /// A closed bursary succeeds with only <see cref="BursaryDetailOutcome.ClosedSummary"/> set, so no eligibility details leave the service.
    /// </summary>
    Task<Result<BursaryDetailOutcome>> GetDetailAsync(string slug, CancellationToken ct);
}

/// <summary>Exactly one of the two is set: <see cref="Detail"/> for an open bursary, <see cref="ClosedSummary"/> for a closed one.</summary>
public sealed record BursaryDetailOutcome(BursaryDetailResponse? Detail, BursarySummaryResponse? ClosedSummary);
