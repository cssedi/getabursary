using GetABursary.Api.DTOs.Tracker;
using GetABursary.Core.Common;

namespace GetABursary.Api.Services.Tracker;

public interface ITrackerService
{
    /// <summary>The user's tracker, soonest closing first. Only ever reads entries of <paramref name="userId"/>.</summary>
    Task<Result<IReadOnlyList<TrackerEntryResponse>>> ListAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Creates or updates the user's entry for a published bursary. Unknown or unpublished bursaries fail with <c>Bursary.NotFound</c>.
    /// </summary>
    Task<Result<UpsertTrackerOutcome>> UpsertAsync(Guid userId, Guid bursaryId, UpsertTrackerRequest request, CancellationToken ct);

    /// <summary>Removes the user's entry for the bursary. Succeeds whether or not an entry existed.</summary>
    Task DeleteAsync(Guid userId, Guid bursaryId, CancellationToken ct);
}

/// <summary><see cref="Created"/> decides between <c>201</c> and <c>200</c>; <see cref="Entry"/> is the body either way.</summary>
public sealed record UpsertTrackerOutcome(bool Created, TrackerEntryResponse Entry);
