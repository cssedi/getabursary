namespace GetABursary.Worker.Pipeline;

/// <summary>The ingestion pipeline for one run. Implemented in T-019.</summary>
public interface IIngestionPipeline
{
    /// <param name="runId">The <c>ingestion_run</c> being executed.</param>
    /// <param name="sourceId">Restrict the run to one source; <c>null</c> means all enabled sources.</param>
    Task RunAsync(Guid runId, Guid? sourceId, CancellationToken ct);
}
