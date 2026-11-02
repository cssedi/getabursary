using System.Security.Claims;
using GetABursary.Api.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GetABursary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me")]
public class MeController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    public ActionResult<MeResponse> Get() => Ok(new MeResponse
    {
        Id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        Email = User.FindFirstValue(ClaimTypes.Email)!,
        Roles = [.. User.FindAll(ClaimTypes.Role).Select(c => c.Value)],
    });
}
