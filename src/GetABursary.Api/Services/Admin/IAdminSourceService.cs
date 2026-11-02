using GetABursary.Api.DTOs.Admin;
using GetABursary.Api.DTOs.Common;
using GetABursary.Core.Common;

namespace GetABursary.Api.Services.Admin;

public interface IAdminSourceService
{
    Task<Result<IReadOnlyList<SourceResponse>>> ListSourcesAsync(CancellationToken ct);

    /// <summary>Switches a source on or off and returns it in the list shape.</summary>
    Task<Result<SourceResponse>> SetEnabledAsync(Guid id, bool? enabled, CancellationToken ct);

    /// <summary>Queues a run for one source (or all enabled sources when <paramref name="sourceId"/> is null). Returns the waiting request if there already is one.</summary>
    Task<Result<IngestionRunRequestResponse>> RequestRunAsync(Guid? sourceId, Guid userId, CancellationToken ct);

    Task<Result<PagedResponse<IngestionRunResponse>>> ListRunsAsync(int page, CancellationToken ct);
}
