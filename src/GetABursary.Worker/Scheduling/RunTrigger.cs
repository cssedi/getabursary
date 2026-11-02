using GetABursary.Core.Common;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Repositories.Ingestion;
using GetABursary.Worker.Pipeline;
using Microsoft.Extensions.Logging;

namespace GetABursary.Worker.Scheduling;

public sealed partial class RunTrigger(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    ILogger<RunTrigger> logger) : IRunTrigger, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<RunStatus> TriggerAsync(IngestionTrigger trigger, Guid? sourceId, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IIngestionRunRepository>();

            var run = await repository.CreateRunAsync(trigger, clock.UtcNow, ct);
            await repository.SaveChangesAsync(ct);

            var status = RunStatus.Succeeded;
            try
            {
                var pipeline = scope.ServiceProvider.GetRequiredService<IIngestionPipeline>();
                await pipeline.RunAsync(run.Id, sourceId, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                status = RunStatus.Failed;
                LogRunCancelled(logger, run.Id);
            }
            catch (Exception ex)
            {
                status = RunStatus.Failed;
                LogRunFailed(logger, ex, run.Id);
            }

            // Use CancellationToken.None so a shutdown does not leave the run stuck in Running.
            await repository.CompleteRunAsync(run.Id, status, clock.UtcNow, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
            return status;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    [LoggerMessage(Level = LogLevel.Error, Message = "Ingestion run {RunId} failed")]
    private static partial void LogRunFailed(ILogger logger, Exception ex, Guid runId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ingestion run {RunId} was cancelled")]
    private static partial void LogRunCancelled(ILogger logger, Guid runId);
}
