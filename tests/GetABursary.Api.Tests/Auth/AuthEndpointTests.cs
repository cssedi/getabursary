using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GetABursary.Api.Tests.Auth;

[Collection(AuthFixtureDefinition.Name)]
public sealed partial class AuthEndpointTests(AuthFixture fixture)
{
    private const string Password = "Passw0rd!";

    [Fact]
    public async Task Flow_RegisterConfirmLogin_MeReturnsEmailAndStudent()
    {
        var email = NewEmail();
        using var client = fixture.Factory.CreateClient();

        await RegisterAsync(client, email);

        using var beforeConfirm = await LoginAsync(client, email, Password);
        Assert.Equal(HttpStatusCode.Unauthorized, beforeConfirm.StatusCode);

        await ConfirmAsync(client, email);

        using var login = await LoginAsync(client, email, Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var setCookie = string.Join(';', login.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);

        var me = await GetMeAsync(client);
        Assert.Equal(email, me.GetProperty("email").GetString());
        Assert.Equal(["Student"], me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task Me_SignedOut_Returns401()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPasswordFiveTimes_LocksOutEvenWithCorrectPassword()
    {
        var email = NewEmail();
        using var client = fixture.Factory.CreateClient();
        await RegisterAsync(client, email);
        await ConfirmAsync(client, email);

        for (var i = 0; i < 5; i++)
        {
            using var bad = await LoginAsync(client, email, "Wrong-pass1!");
            Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
        }

        using var locked = await LoginAsync(client, email, Password);
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Contains("LockedOut", await locked.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Logout_AfterLogin_ClearsSession()
    {
        var email = NewEmail();
        using var client = fixture.Factory.CreateClient();
        await RegisterAsync(client, email);
        await ConfirmAsync(client, email);
        using var login = await LoginAsync(client, email, Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var logout = await client.PostAsync("/api/v1/auth/logout", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        using var me = await client.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Confirm_AdminEmail_GetsAdminAndStudentRoles()
    {
        using var client = fixture.Factory.CreateClient();
        await RegisterAsync(client, AuthFixture.AdminEmail);
        await ConfirmAsync(client, AuthFixture.AdminEmail);
        using var login = await LoginAsync(client, AuthFixture.AdminEmail, Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var me = await GetMeAsync(client);

        var roles = me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()!).Order().ToArray();
        Assert.Equal(["Admin", "Student"], roles);
    }

    [Fact]
    public async Task Post_WithoutRequestedWithHeader_Returns400()
    {
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Remove("X-Requested-With");

        using var response = await client.PostAsync("/api/v1/auth/login", JsonContent.Create(new { email = "a@b.test", password = Password }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmationEmail_LinkPointsToWebApp()
    {
        var email = NewEmail();
        using var client = fixture.Factory.CreateClient();

        await RegisterAsync(client, email);

        Assert.StartsWith("https://web.example.test/account/confirm?userId=", LastLinkFor(email), StringComparison.Ordinal);
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.test";

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsync("/api/v1/auth/register", JsonContent.Create(new { email, password = Password }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsync("/api/v1/auth/login?useCookies=true", JsonContent.Create(new { email, password }));

    private async Task ConfirmAsync(HttpClient client, string email)
    {
        var link = new Uri(LastLinkFor(email));
        using var response = await client.GetAsync("/api/v1/auth/confirmEmail" + link.Query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private string LastLinkFor(string email)
    {
        var message = fixture.Emails.Last(e => e.Contains($"Email to {email} ", StringComparison.Ordinal));
        return UrlPattern().Match(message).Value.Replace("&amp;", "&", StringComparison.Ordinal);
    }

    private static async Task<JsonElement> GetMeAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).Clone();
    }

    [GeneratedRegex(@"https://\S+")]
    private static partial Regex UrlPattern();
}
