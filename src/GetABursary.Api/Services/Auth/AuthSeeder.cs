using GetABursary.Core.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace GetABursary.Api.Services.Auth;

public static class AuthSeeder
{
    /// <summary>
    /// Ensures the Student and Admin roles exist and gives Admin to confirmed users listed in <c>Admin:Emails</c>.
    /// If <c>Admin:InitialPassword</c> is set, a listed admin who has no account yet is created (confirmed) with it,
    /// so the owner can sign in before email is set up. Existing accounts are never changed.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { UserRoles.Student, UserRoles.Admin })
        {
            if (!await roles.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roles.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.CreateVersion7() }));
            }
        }

        var users = services.GetRequiredService<UserManager<User>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var initialPassword = configuration["Admin:InitialPassword"];
        foreach (var email in AdminEmails.From(configuration))
        {
            var user = await users.FindByEmailAsync(email);
            if (user is null && !string.IsNullOrWhiteSpace(initialPassword))
            {
                user = new User { Id = Guid.CreateVersion7(), UserName = email, Email = email, EmailConfirmed = true };
                EnsureSucceeded(await users.CreateAsync(user, initialPassword));
            }

            if (user is { EmailConfirmed: true } && !await users.IsInRoleAsync(user, UserRoles.Admin))
            {
                EnsureSucceeded(await users.AddToRoleAsync(user, UserRoles.Admin));
            }
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Auth seeding failed: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
