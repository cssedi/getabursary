using GetABursary.Core.Common;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Repositories.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GetABursary.Worker.Scheduling;

/// <summary>Starts runs on the cron schedule and whenever an <c>ingestion_run_request</c> appears.</summary>
public sealed partial class IngestionScheduler(
    IScheduleCalculator schedule,
    IRunTrigger runTrigger,
    IServiceScopeFactory scopeFactory,
    IClock clock,
    IOptions<IngestionOptions> options,
    ILogger<IngestionScheduler> logger) : BackgroundService
{
    private DateTimeOffset? _nextScheduled;
    private bool _initialised;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.RequestPollSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            await TickAsync(stoppingToken);
            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>One scheduler iteration: a due scheduled run, then pending manual requests. Never throws.</summary>
    public async Task TickAsync(CancellationToken ct)
    {
        try
        {
            if (!_initialised)
            {
                _nextScheduled = schedule.GetNextOccurrence();
                _initialised = true;
            }

            if (_nextScheduled is { } due && clock.UtcNow >= due)
            {
                _nextScheduled = schedule.GetNextOccurrence();
                await runTrigger.TriggerAsync(IngestionTrigger.Scheduled, null, ct);
            }

            await ProcessRequestsAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            LogTickFailed(logger, ex);
        }
    }

    private async Task ProcessRequestsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIngestionRunRepository>();

        var pending = await repository.GetPendingRequestsAsync(ct);
        foreach (var request in pending)
        {
            if (!await repository.MarkRequestPickedAsync(request.Id, clock.UtcNow, ct))
            {
                continue; // already claimed
            }

            await runTrigger.TriggerAsync(IngestionTrigger.Manual, request.SourceId, ct);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Ingestion scheduler tick failed")]
    private static partial void LogTickFailed(ILogger logger, Exception ex);
}
