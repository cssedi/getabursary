using GetABursary.Api.DTOs.Bursaries;
using GetABursary.Api.DTOs.Common;
using GetABursary.Api.Extensions;
using GetABursary.Api.Services.Bursaries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GetABursary.Api.Controllers;

/// <summary>Bursary discovery (FR-01–FR-06). Anonymous.</summary>
[ApiController]
[Route("api/v1/bursaries")]
public class BursariesController(IBursaryMatchingService matching) : ControllerBase
{
    /// <summary>Bursaries the student is eligible for, ranked by match quality.</summary>
    [HttpGet("search")]
    [ProducesResponseType<PagedResponse<BursarySummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] BursarySearchRequest request, CancellationToken ct = default)
    {
        var result = await matching.SearchAsync(request, ct);

        return result.ToActionResult(this);
    }
}
