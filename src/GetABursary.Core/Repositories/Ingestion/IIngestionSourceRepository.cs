using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;

namespace GetABursary.Core.Repositories.Ingestion;

/// <summary>Sources to ingest and the per-source results of a run.</summary>
public interface IIngestionSourceRepository
{
    /// <summary>Enabled sources ordered by key (read-only). When <paramref name="sourceId"/> is set, only that source, if enabled.</summary>
    Task<IReadOnlyList<Source>> GetEnabledSourcesAsync(Guid? sourceId, CancellationToken ct);

    /// <summary>All sources ordered by key (read-only).</summary>
    Task<IReadOnlyList<Source>> GetAllSourcesAsync(CancellationToken ct);

    /// <summary>One source, tracked: changes to it are persisted by <see cref="SaveChangesAsync"/>.</summary>
    Task<Source?> GetSourceAsync(Guid id, CancellationToken ct);

    /// <summary>The most recent per-source result row for each source that has run, with its run (read-only).</summary>
    Task<IReadOnlyList<IngestionRunSource>> GetLatestRunSourcesAsync(CancellationToken ct);

    /// <summary>Adds a per-source result row. Call <see cref="SaveChangesAsync"/> to persist it.</summary>
    void AddRunSource(IngestionRunSource row);

    /// <summary>Records a status on the (tracked) run. Call <see cref="SaveChangesAsync"/> to persist it.</summary>
    Task SetRunStatusAsync(Guid runId, RunStatus status, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
