using GetABursary.Api.DTOs.Reports;
using GetABursary.Api.Extensions;
using GetABursary.Api.Services.Reports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GetABursary.Api.Controllers;

[ApiController]
[Route("api/v1/bursaries/{id:guid}/reports")]
public class ReportsController(IReportService reports) : ControllerBase
{
    /// <summary>Reports an inaccuracy in a bursary listing. No login required; rate limited per IP.</summary>
    [HttpPost]
    [EnableRateLimiting(ReportsRateLimiting.PolicyName)]
    [ProducesResponseType<CreateReportResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create(Guid id, [FromBody] CreateReportRequest request, CancellationToken ct)
    {
        var result = await reports.CreateReportAsync(id, request, ClientIp.From(HttpContext), ct);

        return result.ToActionResult(this, _ => StatusCodes.Status201Created);
    }
}
