using GetABursary.Core.Data;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Repositories.Ingestion;

public class IngestionSourceRepository(GetABursaryDbContext db) : IIngestionSourceRepository
{
    public async Task<IReadOnlyList<Source>> GetEnabledSourcesAsync(Guid? sourceId, CancellationToken ct) =>
        await db.Sources.AsNoTracking()
            .Where(s => s.Enabled && (sourceId == null || s.Id == sourceId))
            .OrderBy(s => s.Key)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Source>> GetAllSourcesAsync(CancellationToken ct) =>
        await db.Sources.AsNoTracking().OrderBy(s => s.Key).ToListAsync(ct);

    public Task<Source?> GetSourceAsync(Guid id, CancellationToken ct) =>
        db.Sources.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<IngestionRunSource>> GetLatestRunSourcesAsync(CancellationToken ct) =>
        await db.IngestionRunSources.AsNoTracking()
            .Include(rs => rs.Run)
            .Where(rs => !db.IngestionRunSources.Any(o => o.SourceId == rs.SourceId && o.Run.StartedAt > rs.Run.StartedAt))
            .ToListAsync(ct);

    public void AddRunSource(IngestionRunSource row) => db.IngestionRunSources.Add(row);

    public async Task SetRunStatusAsync(Guid runId, RunStatus status, CancellationToken ct)
    {
        var run = await db.IngestionRuns.SingleAsync(r => r.Id == runId, ct);
        run.Status = status;
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
