using System.Security.Claims;
using System.Text.Json;
using GetABursary.Api.DTOs.Admin;
using GetABursary.Api.DTOs.Common;
using GetABursary.Api.Extensions;
using GetABursary.Api.Services.Admin;
using GetABursary.Core.Domain.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GetABursary.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public class AdminController(IAdminService admin) : ControllerBase
{
    [HttpGet("review-items")]
    [ProducesResponseType<PagedResponse<ReviewItemResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListReviewItems([FromQuery] ReviewItemType? type, [FromQuery] int page = 1, CancellationToken ct = default) =>
        (await admin.ListReviewItemsAsync(type, page, ct)).ToActionResult(this);

    [HttpPost("review-items/{id:guid}/resolve")]
    [ProducesResponseType<ReviewItemResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveReviewItemRequest request, CancellationToken ct) =>
        (await admin.ResolveReviewItemAsync(id, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), request, ct)).ToActionResult(this);

    [HttpGet("bursaries")]
    [ProducesResponseType<PagedResponse<AdminBursarySummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchBursaries(
        [FromQuery] string? query, [FromQuery] bool? published, [FromQuery] int page = 1, CancellationToken ct = default) =>
        (await admin.SearchBursariesAsync(query, published, page, ct)).ToActionResult(this);

    [HttpGet("bursaries/{id:guid}")]
    [ProducesResponseType<AdminBursaryResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBursary(Guid id, CancellationToken ct) =>
        (await admin.GetBursaryAsync(id, ct)).ToActionResult(this);

    /// <summary>Partial update: send only the fields to change. Each changed field is locked against re-ingestion.</summary>
    [HttpPatch("bursaries/{id:guid}")]
    [ProducesResponseType<AdminBursaryResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBursary(Guid id, [FromBody] Dictionary<string, JsonElement> changes, CancellationToken ct) =>
        (await admin.UpdateBursaryAsync(id, changes, ct)).ToActionResult(this);

    [HttpDelete("bursaries/{id:guid}/locks/{field}")]
    [ProducesResponseType<AdminBursaryResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Unlock(Guid id, string field, CancellationToken ct) =>
        (await admin.UnlockFieldAsync(id, field, ct)).ToActionResult(this);
}
