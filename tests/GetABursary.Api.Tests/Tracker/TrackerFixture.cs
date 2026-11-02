using System.Net.Http.Json;
using GetABursary.Api.Services.Auth;
using GetABursary.Core.Common;
using GetABursary.Core.Data;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GetABursary.Api.Tests.Tracker;

/// <summary>One PostgreSQL container with three published bursaries (two dated, one undated) and one unpublished bursary.</summary>
public sealed class TrackerFixture : IAsyncLifetime
{
    public const string Password = "Passw0rd!";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public Guid LaterBursaryId { get; private set; }

    public Guid SoonBursaryId { get; private set; }

    public Guid UndatedBursaryId { get; private set; }

    public Guid UnpublishedBursaryId { get; private set; }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
            await SeedAsync(context);
        }

        Factory = new TrackerApiFactory(_container.GetConnectionString());

        // Development hosts with MigrateOnStartup=false skip startup seeding, so create the Student role here.
        using var scope = Factory.Services.CreateScope();
        await AuthSeeder.SeedAsync(scope.ServiceProvider);
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    /// <summary>Creates a confirmed student and signs in with a client that keeps the auth cookie. Dispose the client when done.</summary>
    public async Task<HttpClient> SignInNewUserAsync()
    {
        var email = $"{Guid.CreateVersion7():N}@tracker.test";

        using (var scope = Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var created = await users.CreateAsync(new User { UserName = email, Email = email, EmailConfirmed = true }, Password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException("Test user creation failed: " + string.Join("; ", created.Errors.Select(e => e.Description)));
            }
        }

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "getabursary");

        using var login = await client.PostAsync("/api/v1/auth/login?useCookies=true", JsonContent.Create(new { email, password = Password }));
        login.EnsureSuccessStatusCode();

        return client;
    }

    private GetABursaryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GetABursaryDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options, new SystemClock());

    private async Task SeedAsync(GetABursaryDbContext db)
    {
        var funder = new Funder { Name = "Acme Foundation", NormalizedName = "acme foundation" };
        db.Funders.Add(funder);
        await db.SaveChangesAsync();

        var later = NewBursary("later-bursary", "Later Bursary", new DateOnly(2099, 3, 1), funder.Id, published: true);
        var soon = NewBursary("soon-bursary", "Soon Bursary", new DateOnly(2099, 1, 15), funder.Id, published: true);
        var undated = NewBursary("undated-bursary", "Undated Bursary", null, funder.Id, published: true);
        var unpublished = NewBursary("unpublished-bursary", "Unpublished Bursary", new DateOnly(2099, 2, 1), funder.Id, published: false);
        db.Bursaries.AddRange(later, soon, undated, unpublished);
        await db.SaveChangesAsync();

        LaterBursaryId = later.Id;
        SoonBursaryId = soon.Id;
        UndatedBursaryId = undated.Id;
        UnpublishedBursaryId = unpublished.Id;
    }

    private static Bursary NewBursary(string slug, string name, DateOnly? closingDate, Guid funderId, bool published) => new()
    {
        Slug = slug,
        Name = name,
        NormalizedName = name.ToLowerInvariant(),
        FunderId = funderId,
        Coverage = [CoverageItem.Tuition],
        StudyLevels = [StudyLevel.Undergraduate],
        Status = BursaryStatus.Open,
        ClosingDate = closingDate,
        IsPublished = published,
    };

    private sealed class TrackerApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("ConnectionStrings:Default", connectionString);
            builder.UseSetting("Reports:IpHashSalt", "tracker-test-salt");
            builder.UseSetting("Auth:RateLimitPerHour", "1000");
        }
    }
}

[CollectionDefinition(Name)]
public sealed class TrackerFixtureDefinition : ICollectionFixture<TrackerFixture>
{
    public const string Name = "Tracker";
}
