using System.Net;
using System.Security.Claims;
using GetABursary.Api.Controllers;
using GetABursary.Api.Services.Auth;
using GetABursary.Core.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GetABursary.Api.Tests.Auth;

[Collection(AuthFixtureDefinition.Name)]
public sealed class ExternalLoginTests(AuthFixture fixture)
{
    private static readonly WebApplicationFactoryClientOptions NoRedirect = new() { AllowAutoRedirect = false };

    [Fact]
    public async Task Challenge_WhenGoogleConfigured_RedirectsToGoogle()
    {
        await using var configured = fixture.Factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Auth:Google:ClientId", "test-client-id");
            b.UseSetting("Auth:Google:ClientSecret", "test-client-secret");
        });
        using var client = configured.CreateClient(NoRedirect);

        using var response = await client.GetAsync("/api/v1/auth/external/google?returnUrl=/search?level=Honours");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://accounts.google.com/", response.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Challenge_WhenGoogleNotConfigured_Returns404()
    {
        using var client = fixture.Factory.CreateClient(NoRedirect);

        using var response = await client.GetAsync("/api/v1/auth/external/google");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("https://evil.com", "/")]
    [InlineData("//evil.com", "/")]
    [InlineData("/\\evil.com", "/")]
    [InlineData(null, "/")]
    [InlineData("/search?level=Honours", "/search?level=Honours")]
    public void SafeReturnUrl_AllowsOnlyLocalPaths(string? input, string expected) =>
        Assert.Equal(expected, ExternalAuthController.SafeReturnUrl(input));

    [Theory]
    [InlineData("https://evil.com")]
    [InlineData("//evil.com")]
    public async Task Callback_WithEvilReturnUrl_RedirectsToRoot(string returnUrl)
    {
        using var client = fixture.Factory.CreateClient(NoRedirect);

        using var response = await client.GetAsync($"/api/v1/auth/external/google/callback?returnUrl={Uri.EscapeDataString(returnUrl)}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Resolve_NewGoogleUser_CreatesConfirmedStudent()
    {
        var email = $"new-{Guid.NewGuid():N}@example.test";

        var (user, roles) = await ResolveAsync(Principal("g-" + Guid.NewGuid(), email));

        Assert.Equal(email, user.Email);
        Assert.True(user.EmailConfirmed);
        Assert.Equal(["Student"], roles);
    }

    [Fact]
    public async Task Resolve_AdminEmail_GetsAdminRole()
    {
        var email = $"admin-{Guid.NewGuid():N}@example.test";
        await using var factory = fixture.Factory.WithWebHostBuilder(b => b.UseSetting("Admin:Emails", email));
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IExternalLoginService>();

        var result = await service.ResolveUserAsync(Principal("g-" + Guid.NewGuid(), email), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var roles = await scope.ServiceProvider.GetRequiredService<UserManager<User>>().GetRolesAsync(result.Value);
        Assert.Contains("Admin", roles);
        Assert.Contains("Student", roles);
    }

    [Fact]
    public async Task Resolve_ExistingUnconfirmedEmail_LinksAndConfirms_ThenFindsByLogin()
    {
        var email = $"old-{Guid.NewGuid():N}@example.test";
        var key = "g-" + Guid.NewGuid();
        Guid existingId;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var existing = new User { Id = Guid.CreateVersion7(), UserName = email, Email = email };
            Assert.True((await users.CreateAsync(existing, "Passw0rd!")).Succeeded);
            existingId = existing.Id;
        }

        var (linked, _) = await ResolveAsync(Principal(key, email));
        var (again, _) = await ResolveAsync(Principal(key, email));

        Assert.Equal(existingId, linked.Id);
        Assert.True(linked.EmailConfirmed);
        Assert.Equal(existingId, again.Id);
        using var scope2 = fixture.Factory.Services.CreateScope();
        Assert.False(await scope2.ServiceProvider.GetRequiredService<UserManager<User>>().HasPasswordAsync(linked));
    }

    [Fact]
    public async Task Resolve_UnverifiedEmail_Fails()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IExternalLoginService>();

        var result = await service.ResolveUserAsync(Principal("g-x", "x@example.test", verified: false), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private static ClaimsPrincipal Principal(string key, string email, bool verified = true) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, key),
                new Claim(ClaimTypes.Email, email),
                new Claim("email_verified", verified ? "true" : "false"),
            ],
            "Google"));

    private async Task<(User User, IList<string> Roles)> ResolveAsync(ClaimsPrincipal principal)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IExternalLoginService>();
        var result = await service.ResolveUserAsync(principal, CancellationToken.None);
        Assert.True(result.IsSuccess);
        var roles = await scope.ServiceProvider.GetRequiredService<UserManager<User>>().GetRolesAsync(result.Value);
        return (result.Value, roles);
    }
}
