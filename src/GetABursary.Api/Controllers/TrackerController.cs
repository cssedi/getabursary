using System.Security.Claims;
using GetABursary.Api.DTOs.Tracker;
using GetABursary.Api.Extensions;
using GetABursary.Api.Services.Tracker;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GetABursary.Api.Controllers;

/// <summary>Saved bursaries and the application tracker (FR-12–FR-14). Signed in only; every action works on the caller's own entries.</summary>
[ApiController]
[Authorize]
[Route("api/v1/me/tracker")]
public class TrackerController(ITrackerService tracker) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>The caller's tracker, soonest closing first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TrackerEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await tracker.ListAsync(CurrentUserId, ct);

        return result.ToActionResult(this);
    }

    /// <summary>Saves a bursary or updates its status, date and notes. <c>201</c> when the entry is new, <c>200</c> when updated.</summary>
    [HttpPut("{bursaryId:guid}")]
    [ProducesResponseType<TrackerEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<TrackerEntryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upsert(Guid bursaryId, [FromBody] UpsertTrackerRequest request, CancellationToken ct)
    {
        var result = await tracker.UpsertAsync(CurrentUserId, bursaryId, request, ct);
        if (!result.IsSuccess)
        {
            return result.ToActionResult(this);
        }

        var outcome = result.Value;

        return outcome.Created
            ? StatusCode(StatusCodes.Status201Created, outcome.Entry)
            : Ok(outcome.Entry);
    }

    /// <summary>Removes a bursary from the caller's tracker. Idempotent: <c>204</c> even when nothing was saved.</summary>
    [HttpDelete("{bursaryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid bursaryId, CancellationToken ct)
    {
        await tracker.DeleteAsync(CurrentUserId, bursaryId, ct);

        return NoContent();
    }
}
