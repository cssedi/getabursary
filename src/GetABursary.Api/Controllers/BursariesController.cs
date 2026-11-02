using GetABursary.Api.DTOs.Bursaries;
using GetABursary.Api.DTOs.Common;
using GetABursary.Api.Extensions;
using GetABursary.Api.Services.Bursaries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GetABursary.Api.Controllers;

/// <summary>Bursary discovery (FR-01–FR-07). Anonymous.</summary>
[ApiController]
[Route("api/v1/bursaries")]
public class BursariesController(IBursaryMatchingService matching, IBursaryService bursaries) : ControllerBase
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

    /// <summary>Full detail of an open bursary. A closed bursary returns <c>410 Gone</c> with only its summary in <c>bursary</c>.</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType<BursaryDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status410Gone)]
    public async Task<IActionResult> GetDetail(string slug, CancellationToken ct = default)
    {
        var result = await bursaries.GetDetailAsync(slug, ct);
        if (!result.IsSuccess)
        {
            return result.ToActionResult(this);
        }

        var outcome = result.Value;

        return outcome.ClosedSummary is { } summary
            ? this.ToGoneActionResult(BursaryErrors.Closed(slug), "bursary", summary)
            : Ok(outcome.Detail);
    }
}
