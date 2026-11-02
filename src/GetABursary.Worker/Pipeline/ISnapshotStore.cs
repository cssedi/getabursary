namespace GetABursary.Worker.Pipeline;

public sealed record SnapshotResult(Guid RawListingId, bool Changed);

public interface ISnapshotStore
{
    Task<SnapshotResult> SaveAsync(Guid sourceId, Uri url, string html, CancellationToken ct);
}
