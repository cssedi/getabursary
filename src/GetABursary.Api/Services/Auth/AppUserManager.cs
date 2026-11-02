using GetABursary.Core.Data;
using GetABursary.Core.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GetABursary.Api.Services.Auth;

/// <summary>
/// Gives every new user the <c>Student</c> role and promotes users listed in <c>Admin:Emails</c>
/// to <c>Admin</c> the moment their email is confirmed.
/// </summary>
public class AppUserManager(
    IUserStore<User> store,
    IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<User> passwordHasher,
    IEnumerable<IUserValidator<User>> userValidators,
    IEnumerable<IPasswordValidator<User>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<UserManager<User>> logger,
    IConfiguration configuration,
    GetABursaryDbContext dbContext)
    : UserManager<User>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
{
    public override async Task<IdentityResult> CreateAsync(User user)
    {
        // One transaction so a failed role save never leaves a user without a role.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(CancellationToken);
        var result = await base.CreateAsync(user);
        if (!result.Succeeded)
        {
            return result;
        }

        result = await AddToRoleAsync(user, UserRoles.Student);
        if (result.Succeeded)
        {
            await transaction.CommitAsync(CancellationToken);
        }

        return result;
    }

    public override async Task<IdentityResult> ConfirmEmailAsync(User user, string token)
    {
        var result = await base.ConfirmEmailAsync(user, token);
        if (result.Succeeded && AdminEmails.Contains(configuration, user.Email))
        {
            return await AddToRoleAsync(user, UserRoles.Admin);
        }

        return result;
    }
}
