using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Worker.Scheduling;

/// <summary>Starts one ingestion run and records it. Only one run executes at a time.</summary>
public interface IRunTrigger
{
    /// <summary>Runs the pipeline and returns the final status. Never throws for pipeline failures.</summary>
    Task<RunStatus> TriggerAsync(IngestionTrigger trigger, Guid? sourceId, CancellationToken ct);
}
