using System.Security.Claims;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace GetABursary.Api.Services.Auth;

public interface IExternalLoginService
{
    /// <summary>Finds the user for a Google principal (by login, then by email), or creates one, and applies roles.</summary>
    Task<Result<User>> ResolveUserAsync(ClaimsPrincipal principal, CancellationToken ct);
}

public class ExternalLoginService(UserManager<User> users, IConfiguration configuration) : IExternalLoginService
{
    public const string Provider = "Google";

    public async Task<Result<User>> ResolveUserAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ct.ThrowIfCancellationRequested();

        var key = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (key is null || string.IsNullOrWhiteSpace(email) || !string.Equals(principal.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("Auth.GoogleEmailUnverified", "Google did not return a verified email address.");
        }

        var user = await users.FindByLoginAsync(Provider, key);
        if (user is null)
        {
            user = await users.FindByEmailAsync(email);
            if (user is null)
            {
                user = new User { Id = Guid.CreateVersion7(), UserName = email, Email = email, EmailConfirmed = true };
                var created = await users.CreateAsync(user);
                if (!created.Succeeded)
                {
                    return Failed(created);
                }
            }
            else if (!user.EmailConfirmed)
            {
                // Google verified this address, so it is safe to confirm.
                user.EmailConfirmed = true;
                var updated = await users.UpdateAsync(user);
                if (!updated.Succeeded)
                {
                    return Failed(updated);
                }
            }

            var linked = await users.AddLoginAsync(user, new UserLoginInfo(Provider, key, Provider));
            if (!linked.Succeeded)
            {
                return Failed(linked);
            }
        }

        if (AdminEmails.Contains(configuration, user.Email) && !await users.IsInRoleAsync(user, UserRoles.Admin))
        {
            var promoted = await users.AddToRoleAsync(user, UserRoles.Admin);
            if (!promoted.Succeeded)
            {
                return Failed(promoted);
            }
        }

        return user;
    }

    private static Error Failed(IdentityResult result) =>
        Error.Validation("Auth.GoogleSignInFailed", string.Join(' ', result.Errors.Select(e => e.Description)));
}
