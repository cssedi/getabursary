using GetABursary.Core.Domain.Ingestion;

namespace GetABursary.Core.Repositories.Ingestion;

/// <summary>A listing that has now missed enough runs to be considered vanished.</summary>
public sealed record MissedListing(Guid RawListingId, Guid BursaryId, string Url);

public interface IRawListingRepository
{
    /// <summary>The tracked snapshot for (source, url), or <c>null</c>.</summary>
    Task<RawListing?> GetBySourceAndUrlAsync(Guid sourceId, string url, CancellationToken ct);

    /// <summary>The tracked snapshot with this id, or <c>null</c>.</summary>
    Task<RawListing?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Adds a new snapshot. Call <see cref="SaveChangesAsync"/> to persist it.</summary>
    void Add(RawListing listing);

    /// <summary>
    /// Marks an unchanged listing as seen this run: resets <c>missed_runs</c> and bumps <c>last_seen_at</c> on its
    /// bursary-source link. Takes effect immediately.
    /// </summary>
    Task MarkSeenAsync(Guid rawListingId, DateTimeOffset now, CancellationToken ct);

    /// <summary>
    /// Adds one to <c>missed_runs</c> of every listing of the source that is not in <paramref name="seenIds"/>, and returns
    /// those that have just reached exactly <paramref name="threshold"/> missed runs and belong to a bursary. Takes effect immediately.
    /// </summary>
    Task<IReadOnlyList<MissedListing>> IncrementMissedRunsAsync(
        Guid sourceId, IReadOnlyCollection<Guid> seenIds, int threshold, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
