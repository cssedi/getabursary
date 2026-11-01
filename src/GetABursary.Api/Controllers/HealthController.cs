using GetABursary.Api.DTOs.Health;
using Microsoft.AspNetCore.Mvc;

namespace GetABursary.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get() => Ok(new HealthResponse { Status = "ok" });
}
