using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Users;

namespace GetABursary.Core.Repositories.Tracker;

/// <summary>Data access for saved bursaries and the application tracker. Contains no business rules.</summary>
public interface ITrackerRepository
{
    /// <summary>The user's entries with each bursary (and its funder and sources) loaded in the same query, soonest closing first.</summary>
    Task<IReadOnlyList<TrackerEntry>> ListAsync(Guid userId, CancellationToken ct);

    /// <summary>The published bursary with its funder and sources loaded; null when unknown or unpublished.</summary>
    Task<Bursary?> GetPublishedBursaryAsync(Guid bursaryId, CancellationToken ct);

    /// <summary>The user's entry for the bursary, tracked so changes are written by <see cref="TrySaveChangesAsync"/>; null when absent.</summary>
    Task<TrackerEntry?> FindAsync(Guid userId, Guid bursaryId, CancellationToken ct);

    /// <summary>Tracks a new entry. Nothing is written until <see cref="TrySaveChangesAsync"/>.</summary>
    void Add(TrackerEntry entry);

    /// <summary>Deletes the user's entry for the bursary if there is one. Writes immediately; a missing entry is not an error.</summary>
    Task DeleteAsync(Guid userId, Guid bursaryId, CancellationToken ct);

    /// <summary>Writes all tracked changes in one transaction. Returns false (and untracks the pending inserts) when a unique index rejected them, i.e. a concurrent save won.</summary>
    Task<bool> TrySaveChangesAsync(CancellationToken ct);
}
