using GetABursary.Api.DTOs.Admin;
using GetABursary.Api.DTOs.Common;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Ingestion;
using GetABursary.Core.Repositories.Ingestion;

namespace GetABursary.Api.Services.Admin;

public sealed class AdminSourceService(
    IIngestionSourceRepository sources,
    IIngestionRunRepository runs,
    IClock clock) : IAdminSourceService
{
    public const int RunPageSize = 20;

    public async Task<Result<IReadOnlyList<SourceResponse>>> ListSourcesAsync(CancellationToken ct)
    {
        var all = await sources.GetAllSourcesAsync(ct);
        var latest = await LatestBySourceAsync(ct);
        return all.Select(s => ToSourceResponse(s, latest)).ToList();
    }

    public async Task<Result<SourceResponse>> SetEnabledAsync(Guid id, bool? enabled, CancellationToken ct)
    {
        if (enabled is null)
        {
            return AdminErrors.InvalidEnabled();
        }

        var source = await sources.GetSourceAsync(id, ct);
        if (source is null)
        {
            return AdminErrors.SourceNotFound(id);
        }

        source.Enabled = enabled.Value;
        await sources.SaveChangesAsync(ct);
        return ToSourceResponse(source, await LatestBySourceAsync(ct));
    }

    public async Task<Result<IngestionRunRequestResponse>> RequestRunAsync(Guid? sourceId, Guid userId, CancellationToken ct)
    {
        if (sourceId is { } id && await sources.GetSourceAsync(id, ct) is null)
        {
            return AdminErrors.SourceNotFound(id);
        }

        // shortcut: check-then-insert, so two simultaneous clicks can queue two requests. The worker runs once per picked request; a partial unique index would close this (needs a migration).
        var request = await runs.FindPendingRequestAsync(sourceId, ct);
        if (request is null)
        {
            request = new IngestionRunRequest { SourceId = sourceId, RequestedByUserId = userId, RequestedAt = clock.UtcNow };
            runs.AddRunRequest(request);
            await runs.SaveChangesAsync(ct);
        }

        return ToRequestResponse(request);
    }

    public async Task<Result<PagedResponse<IngestionRunResponse>>> ListRunsAsync(int page, CancellationToken ct)
    {
        if (page < 1)
        {
            return AdminErrors.InvalidPage();
        }

        var (items, total) = await runs.ListRunsAsync((page - 1) * RunPageSize, RunPageSize, ct);
        return new PagedResponse<IngestionRunResponse>
        {
            Items = [.. items.Select(ToRunResponse)],
            Page = page,
            PageSize = RunPageSize,
            TotalCount = total,
        };
    }

    private async Task<Dictionary<Guid, IngestionRunSource>> LatestBySourceAsync(CancellationToken ct) =>
        (await sources.GetLatestRunSourcesAsync(ct)).DistinctBy(r => r.SourceId).ToDictionary(r => r.SourceId);

    private static SourceResponse ToSourceResponse(Source s, Dictionary<Guid, IngestionRunSource> latest) => new()
    {
        Id = s.Id,
        Key = s.Key,
        Name = s.Name,
        Enabled = s.Enabled,
        LastRun = latest.TryGetValue(s.Id, out var row) ? ToLastRunResponse(row) : null,
    };

    private static LastRunResponse ToLastRunResponse(IngestionRunSource row) => new()
    {
        StartedAt = row.Run.StartedAt,
        FinishedAt = row.Run.FinishedAt,
        Status = row.Run.Status.ToString(),
        Discovered = row.Discovered,
        Created = row.Created,
        Updated = row.Updated,
        Failed = row.Failed,
        ErrorSummary = row.ErrorSummary,
    };

    private static IngestionRunRequestResponse ToRequestResponse(IngestionRunRequest r) => new()
    {
        Id = r.Id,
        SourceId = r.SourceId,
        RequestedAt = r.RequestedAt,
        PickedUpAt = r.PickedUpAt,
    };

    private static IngestionRunResponse ToRunResponse(IngestionRun run) => new()
    {
        Id = run.Id,
        Trigger = run.Trigger.ToString(),
        Status = run.Status.ToString(),
        StartedAt = run.StartedAt,
        FinishedAt = run.FinishedAt,
        Sources = [.. run.Sources.OrderBy(s => s.Source.Key).Select(s => new RunSourceResponse
        {
            SourceId = s.SourceId,
            SourceKey = s.Source.Key,
            SourceName = s.Source.Name,
            Discovered = s.Discovered,
            Created = s.Created,
            Updated = s.Updated,
            Failed = s.Failed,
            ErrorSummary = s.ErrorSummary,
        })],
    };
}
