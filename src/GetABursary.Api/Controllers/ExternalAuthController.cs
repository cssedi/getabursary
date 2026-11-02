using GetABursary.Api.Services.Auth;
using GetABursary.Core.Domain.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GetABursary.Api.Controllers;

/// <summary>"Continue with Google" (FR-10): challenge, then the callback that signs the user in.</summary>
[ApiController]
[Route("api/v1/auth/external/google")]
public partial class ExternalAuthController(
    IAuthenticationSchemeProvider schemes,
    IExternalLoginService externalLogin,
    SignInManager<User> signInManager,
    ILogger<ExternalAuthController> logger) : ControllerBase
{
    /// <summary>Open-redirect protection: only local paths ("/x"), never "//host", "/\host" or absolute URLs.</summary>
    public static string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && returnUrl[0] == '/' && (returnUrl.Length == 1 || (returnUrl[1] != '/' && returnUrl[1] != '\\'))
            ? returnUrl
            : "/";

    /// <summary>Starts the Google sign-in. <c>404</c> when Google is not configured.</summary>
    [HttpGet]
    public async Task<IActionResult> Start([FromQuery] string? returnUrl)
    {
        if (await schemes.GetSchemeAsync(GoogleDefaults.AuthenticationScheme) is null)
        {
            return NotFound();
        }

        var callback = Url.Action(nameof(Callback), new { returnUrl = SafeReturnUrl(returnUrl) })!;
        return Challenge(new AuthenticationProperties { RedirectUri = callback }, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? returnUrl, CancellationToken ct)
    {
        var target = SafeReturnUrl(returnUrl);
        var external = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!external.Succeeded)
        {
            LogNoExternalIdentity();
            return LocalRedirect(target);
        }

        var result = await externalLogin.ResolveUserAsync(external.Principal, ct);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        if (result.IsSuccess)
        {
            await signInManager.SignInAsync(result.Value, isPersistent: false);
        }
        else
        {
            LogSignInFailed(result.Errors[0].Code);
        }

        return LocalRedirect(target);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Google sign-in failed: {ErrorCode}")]
    private partial void LogSignInFailed(string errorCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Google sign-in callback had no external identity")]
    private partial void LogNoExternalIdentity();
}
