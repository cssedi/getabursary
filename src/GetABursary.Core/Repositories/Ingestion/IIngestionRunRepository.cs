using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;

namespace GetABursary.Core.Repositories.Ingestion;

public interface IIngestionRunRepository
{
    /// <summary>Adds a new run in <see cref="RunStatus.Running"/>. Call <see cref="SaveChangesAsync"/> to persist it.</summary>
    Task<IngestionRun> CreateRunAsync(IngestionTrigger trigger, DateTimeOffset startedAt, CancellationToken ct);

    /// <summary>Sets the final status and finish time of a run. Call <see cref="SaveChangesAsync"/> to persist it.</summary>
    Task CompleteRunAsync(Guid runId, RunStatus status, DateTimeOffset finishedAt, CancellationToken ct);

    /// <summary>Requests that have not been picked up yet, oldest first.</summary>
    Task<IReadOnlyList<IngestionRunRequest>> GetPendingRequestsAsync(CancellationToken ct);

    /// <summary>
    /// Atomically marks a request as picked up. Returns <c>false</c> if it was already picked up,
    /// so a request can only ever be claimed once. Takes effect immediately.
    /// </summary>
    Task<bool> MarkRequestPickedAsync(Guid requestId, DateTimeOffset pickedUpAt, CancellationToken ct);

    /// <summary>The oldest unpicked request for the same source (null = all sources), or null if none is waiting.</summary>
    Task<IngestionRunRequest?> FindPendingRequestAsync(Guid? sourceId, CancellationToken ct);

    /// <summary>Adds a manual run request. Call <see cref="SaveChangesAsync"/> to persist it.</summary>
    void AddRunRequest(IngestionRunRequest request);

    /// <summary>A page of runs, newest first, with their per-source rows and source names (read-only).</summary>
    Task<(IReadOnlyList<IngestionRun> Items, int Total)> ListRunsAsync(int skip, int take, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
