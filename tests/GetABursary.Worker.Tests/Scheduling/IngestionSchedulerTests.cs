using GetABursary.Core.Common;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;
using GetABursary.Core.Repositories.Ingestion;
using GetABursary.Worker.Pipeline;
using GetABursary.Worker.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GetABursary.Worker.Tests.Scheduling;

public class IngestionSchedulerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TickAsync_PipelineThrows_RunIsFailedAndSchedulerContinues()
    {
        var h = new Harness(new ThrowingPipeline());
        h.Repository.Requests.Add(new IngestionRunRequest { SourceId = Guid.NewGuid(), RequestedAt = Now });
        h.Repository.Requests.Add(new IngestionRunRequest { RequestedAt = Now.AddSeconds(1) });

        await h.Scheduler.TickAsync(CancellationToken.None); // must not throw

        Assert.Equal(2, h.Repository.Runs.Count);
        Assert.All(h.Repository.Runs, r =>
        {
            Assert.Equal(RunStatus.Failed, r.Status);
            Assert.NotNull(r.FinishedAt);
        });

        // Scheduler is still usable afterwards.
        h.Repository.Requests.Add(new IngestionRunRequest { RequestedAt = Now.AddSeconds(2) });
        await h.Scheduler.TickAsync(CancellationToken.None);
        Assert.Equal(3, h.Repository.Runs.Count);
    }

    [Fact]
    public async Task TickAsync_PendingRequest_IsPickedOnceAndRunsWithSourceId()
    {
        var pipeline = new RecordingPipeline();
        var h = new Harness(pipeline);
        var sourceId = Guid.NewGuid();
        h.Repository.Requests.Add(new IngestionRunRequest { SourceId = sourceId, RequestedAt = Now });

        await h.Scheduler.TickAsync(CancellationToken.None);
        await h.Scheduler.TickAsync(CancellationToken.None);

        var run = Assert.Single(h.Repository.Runs);
        Assert.Equal(IngestionTrigger.Manual, run.Trigger);
        Assert.Equal(RunStatus.Succeeded, run.Status);
        var call = Assert.Single(pipeline.Calls);
        Assert.Equal(sourceId, call.SourceId);
        Assert.Equal(run.Id, call.RunId);
    }

    [Fact]
    public async Task TickAsync_ScheduledTimeReached_StartsScheduledRunOnce()
    {
        var h = new Harness(new RecordingPipeline());
        await h.Scheduler.TickAsync(CancellationToken.None); // initialises next occurrence
        Assert.Empty(h.Repository.Runs);

        h.Clock.Advance(TimeSpan.FromHours(20)); // past 03:00 SAST next day
        await h.Scheduler.TickAsync(CancellationToken.None);
        await h.Scheduler.TickAsync(CancellationToken.None);

        var run = Assert.Single(h.Repository.Runs);
        Assert.Equal(IngestionTrigger.Scheduled, run.Trigger);
    }

    private sealed class Harness
    {
        public FakeClock Clock { get; } = new(Now);

        public FakeRunRepository Repository { get; } = new();

        public IngestionScheduler Scheduler { get; }

        public Harness(IIngestionPipeline pipeline)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IClock>(Clock);
            services.AddSingleton<IIngestionRunRepository>(Repository);
            services.AddSingleton(pipeline);
            var provider = services.BuildServiceProvider();

            var options = Options.Create(new IngestionOptions());
            var scopes = provider.GetRequiredService<IServiceScopeFactory>();
            var trigger = new RunTrigger(scopes, Clock, NullLogger<RunTrigger>.Instance);
            Scheduler = new IngestionScheduler(
                new ScheduleCalculator(Clock, options),
                trigger,
                scopes,
                Clock,
                options,
                NullLogger<IngestionScheduler>.Instance);
        }
    }

    private sealed class FakeClock(DateTimeOffset start) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = start;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);

        public void Advance(TimeSpan by) => UtcNow += by;
    }

    private sealed class ThrowingPipeline : IIngestionPipeline
    {
        public Task RunAsync(Guid runId, Guid? sourceId, CancellationToken ct) =>
            throw new InvalidOperationException("boom");
    }

    private sealed class RecordingPipeline : IIngestionPipeline
    {
        public List<(Guid RunId, Guid? SourceId)> Calls { get; } = [];

        public Task RunAsync(Guid runId, Guid? sourceId, CancellationToken ct)
        {
            Calls.Add((runId, sourceId));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRunRepository : IIngestionRunRepository
    {
        public List<IngestionRun> Runs { get; } = [];

        public List<IngestionRunRequest> Requests { get; } = [];

        public Task<IngestionRun> CreateRunAsync(IngestionTrigger trigger, DateTimeOffset startedAt, CancellationToken ct)
        {
            var run = new IngestionRun { Trigger = trigger, StartedAt = startedAt };
            Runs.Add(run);
            return Task.FromResult(run);
        }

        public Task CompleteRunAsync(Guid runId, RunStatus status, DateTimeOffset finishedAt, CancellationToken ct)
        {
            var run = Runs.Single(r => r.Id == runId);
            run.Status = status;
            run.FinishedAt = finishedAt;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<IngestionRunRequest>> GetPendingRequestsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<IngestionRunRequest>>(
                Requests.Where(r => r.PickedUpAt is null).OrderBy(r => r.RequestedAt).ToList());

        public Task<bool> MarkRequestPickedAsync(Guid requestId, DateTimeOffset pickedUpAt, CancellationToken ct)
        {
            var request = Requests.Single(r => r.Id == requestId);
            if (request.PickedUpAt is not null)
            {
                return Task.FromResult(false);
            }

            request.PickedUpAt = pickedUpAt;
            return Task.FromResult(true);
        }

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
