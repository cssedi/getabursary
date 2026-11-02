using GetABursary.Core.Data;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Repositories.Ingestion;

public class IngestionRunRepository(GetABursaryDbContext db) : IIngestionRunRepository
{
    public Task<IngestionRun> CreateRunAsync(IngestionTrigger trigger, DateTimeOffset startedAt, CancellationToken ct)
    {
        var run = new IngestionRun { Trigger = trigger, Status = RunStatus.Running, StartedAt = startedAt };
        db.IngestionRuns.Add(run);
        return Task.FromResult(run);
    }

    public async Task CompleteRunAsync(Guid runId, RunStatus status, DateTimeOffset finishedAt, CancellationToken ct)
    {
        var run = await db.IngestionRuns.SingleAsync(r => r.Id == runId, ct);
        run.Status = status;
        run.FinishedAt = finishedAt;
    }

    public async Task<IReadOnlyList<IngestionRunRequest>> GetPendingRequestsAsync(CancellationToken ct) =>
        await db.IngestionRunRequests
            .AsNoTracking()
            .Where(r => r.PickedUpAt == null)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync(ct);

    public async Task<bool> MarkRequestPickedAsync(Guid requestId, DateTimeOffset pickedUpAt, CancellationToken ct)
    {
        var updated = await db.IngestionRunRequests
            .Where(r => r.Id == requestId && r.PickedUpAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.PickedUpAt, pickedUpAt)
                .SetProperty(r => r.UpdatedAt, pickedUpAt), ct);
        return updated == 1;
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
