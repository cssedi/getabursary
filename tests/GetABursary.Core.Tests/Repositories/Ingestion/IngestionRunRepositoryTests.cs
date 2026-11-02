using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;
using GetABursary.Core.Repositories.Ingestion;
using GetABursary.Core.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Tests.Repositories.Ingestion;

[Collection("Postgres")]
public class IngestionRunRepositoryTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateRunAsync_ThenCompleteRunAsync_PersistsStatusAndFinishTime()
    {
        Guid runId;
        await using (var db = fixture.CreateContext())
        {
            var repo = new IngestionRunRepository(db);
            var run = await repo.CreateRunAsync(IngestionTrigger.Scheduled, Now, CancellationToken.None);
            await repo.SaveChangesAsync(CancellationToken.None);
            runId = run.Id;
        }

        await using (var db = fixture.CreateContext())
        {
            var repo = new IngestionRunRepository(db);
            await repo.CompleteRunAsync(runId, RunStatus.Failed, Now.AddMinutes(5), CancellationToken.None);
            await repo.SaveChangesAsync(CancellationToken.None);
        }

        await using var reader = fixture.CreateContext();
        var saved = await reader.IngestionRuns.AsNoTracking().SingleAsync(r => r.Id == runId);
        Assert.Equal(IngestionTrigger.Scheduled, saved.Trigger);
        Assert.Equal(RunStatus.Failed, saved.Status);
        Assert.Equal(Now, saved.StartedAt);
        Assert.Equal(Now.AddMinutes(5), saved.FinishedAt);
    }

    [Fact]
    public async Task MarkRequestPickedAsync_CalledTwice_SucceedsOnlyOnce()
    {
        var source = new Source { Key = "src-" + Guid.NewGuid().ToString("N"), Name = "Req Source", BaseUrl = "https://req.test" };
        var request = new IngestionRunRequest
        {
            Source = source,
            RequestedByUserId = Guid.NewGuid(),
            RequestedAt = Now,
        };
        await using (var db = fixture.CreateContext())
        {
            db.IngestionRunRequests.Add(request);
            await db.SaveChangesAsync();
        }

        await using var ctx1 = fixture.CreateContext();
        await using var ctx2 = fixture.CreateContext();
        var repo1 = new IngestionRunRepository(ctx1);
        var repo2 = new IngestionRunRepository(ctx2);

        var pending = await repo1.GetPendingRequestsAsync(CancellationToken.None);
        var found = Assert.Single(pending, r => r.Id == request.Id);
        Assert.Equal(source.Id, found.SourceId);

        var first = await repo1.MarkRequestPickedAsync(request.Id, Now.AddMinutes(1), CancellationToken.None);
        var second = await repo2.MarkRequestPickedAsync(request.Id, Now.AddMinutes(2), CancellationToken.None);

        Assert.True(first);
        Assert.False(second);
        Assert.DoesNotContain(await repo2.GetPendingRequestsAsync(CancellationToken.None), r => r.Id == request.Id);

        await using var reader = fixture.CreateContext();
        var saved = await reader.IngestionRunRequests.AsNoTracking().SingleAsync(r => r.Id == request.Id);
        Assert.Equal(Now.AddMinutes(1), saved.PickedUpAt);
    }
}
