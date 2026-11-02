using GetABursary.Worker.Pipeline;

namespace GetABursary.Worker.Scheduling;

/// <summary>Placeholder pipeline registered until T-019 provides the real one.</summary>
public sealed class NoOpIngestionPipeline : IIngestionPipeline
{
    public Task RunAsync(Guid runId, Guid? sourceId, CancellationToken ct) => Task.CompletedTask;
}
