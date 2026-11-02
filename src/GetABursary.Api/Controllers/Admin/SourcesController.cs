using System.Security.Claims;
using GetABursary.Api.DTOs.Admin;
using GetABursary.Api.DTOs.Common;
using GetABursary.Api.Extensions;
using GetABursary.Api.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GetABursary.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public class SourcesController(IAdminSourceService sourceService) : ControllerBase
{
    [HttpGet("sources")]
    [ProducesResponseType<SourceResponse[]>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSources(CancellationToken ct)
    {
        var result = await sourceService.ListSourcesAsync(ct);

        return result.ToActionResult(this);
    }

    [HttpPatch("sources/{id:guid}")]
    [ProducesResponseType<SourceResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSource(Guid id, [FromBody] UpdateSourceRequest request, CancellationToken ct)
    {
        var result = await sourceService.SetEnabledAsync(id, request.Enabled, ct);

        return result.ToActionResult(this);
    }

    /// <summary>Asks the worker for a run. The worker picks the request up within a minute.</summary>
    [HttpPost("ingestion-runs")]
    [ProducesResponseType<IngestionRunRequestResponse>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RequestRun([FromBody] CreateIngestionRunRequest request, CancellationToken ct)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await sourceService.RequestRunAsync(request.SourceId, adminId, ct);

        return result.ToActionResult(this, _ => StatusCodes.Status202Accepted);
    }

    [HttpGet("ingestion-runs")]
    [ProducesResponseType<PagedResponse<IngestionRunResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRuns([FromQuery] int page = 1, CancellationToken ct = default)
    {
        var result = await sourceService.ListRunsAsync(page, ct);

        return result.ToActionResult(this);
    }
}
