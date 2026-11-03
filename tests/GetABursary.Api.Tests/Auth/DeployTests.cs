using System.Net;
using System.Net.Http.Json;
using GetABursary.Api;
using GetABursary.Core.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GetABursary.Api.Tests.Auth;

public sealed class DeployTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Migrate_EmptyDatabase_CreatesSchemaAndSeed()
    {
        await using var factory = NewFactory();

        await MigrateMode.RunAsync(factory.Services);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.True(await db.Institutions.AnyAsync());
        Assert.True(await db.Sources.AnyAsync());
        Assert.True(await db.Roles.AnyAsync());
    }

    [Fact]
    public async Task DataProtection_KeysInDatabase_CookieReadableByASecondInstance()
    {
        await using var first = NewFactory();
        await MigrateMode.RunAsync(first.Services);
        using var client = first.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "getabursary");
        var email = $"dp-{Guid.NewGuid():N}@example.test";

        using var register = await client.PostAsync("/api/v1/auth/register", JsonContent.Create(new { email, password = "Passw0rd!" }));
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        using (var scope = first.Services.CreateScope())
        {
            // Development uses LogEmailSender, so the confirm link is only in the log. Confirm directly instead.
            var db = scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>();
            await db.Users.Where(u => u.Email == email).ExecuteUpdateAsync(s => s.SetProperty(u => u.EmailConfirmed, true));
        }

        using var login = await client.PostAsync("/api/v1/auth/login?useCookies=true", JsonContent.Create(new { email, password = "Passw0rd!" }));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = login.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(".AspNetCore.Identity.Application=", StringComparison.Ordinal)).Split(';')[0];

        using (var scope = first.Services.CreateScope())
        {
            Assert.True(await scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>().DataProtectionKeys.AnyAsync());
        }

        await using var second = NewFactory();
        using var other = second.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        request.Headers.Add("Cookie", cookie);
        using var me = await other.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    private WebApplicationFactory<Program> NewFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("ConnectionStrings:Default", _container.GetConnectionString());
            builder.UseSetting("Reports:IpHashSalt", "deploy-test-salt");
            builder.UseSetting("App:PublicBaseUrl", "https://web.example.test");
        });
}
