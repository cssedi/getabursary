using System.Collections.Concurrent;
using GetABursary.Api.Services.Auth;
using GetABursary.Core.Common;
using GetABursary.Core.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace GetABursary.Api.Tests.Auth;

/// <summary>A throwaway PostgreSQL container plus API hosts that capture the emails written by <c>LogEmailSender</c>.</summary>
public sealed class AuthFixture : IAsyncLifetime
{
    public const string AdminEmail = "boss@example.test";

    /// <summary>A second admin so the admin endpoint tests never collide with the auth tests that register <see cref="AdminEmail"/>.</summary>
    public const string AdminTestsEmail = "admin-tests@example.test";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public ConcurrentQueue<string> Emails { get; } = new();

    public AuthApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = new GetABursaryDbContext(
            new DbContextOptionsBuilder<GetABursaryDbContext>()
                .UseNpgsql(_container.GetConnectionString())
                .UseSnakeCaseNamingConvention()
                .Options,
            new SystemClock());
        await context.Database.MigrateAsync();
        Factory = new AuthApiFactory(_container.GetConnectionString(), Emails);

        // Development hosts with MigrateOnStartup=false skip startup seeding, so run it here.
        using var scope = Factory.Services.CreateScope();
        await AuthSeeder.SeedAsync(scope.ServiceProvider);
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    public sealed class AuthApiFactory(string connectionString, ConcurrentQueue<string> emails) : WebApplicationFactory<Program>
    {
        protected override void ConfigureClient(HttpClient client) =>
            client.DefaultRequestHeaders.Add("X-Requested-With", "getabursary");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("ConnectionStrings:Default", connectionString);
            builder.UseSetting("Reports:IpHashSalt", "auth-test-salt");
            builder.UseSetting("Admin:Emails", $"{AdminEmail},{AdminTestsEmail}");
            builder.UseSetting("App:PublicBaseUrl", "https://web.example.test");
            builder.ConfigureLogging(logging => logging.AddProvider(new CaptureProvider(emails)));
        }
    }

    private sealed class CaptureProvider(ConcurrentQueue<string> emails) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(categoryName, emails);

        public void Dispose()
        {
        }
    }

    private sealed class CaptureLogger(string category, ConcurrentQueue<string> emails) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (category.EndsWith("LogEmailSender", StringComparison.Ordinal))
            {
                emails.Enqueue(formatter(state, exception));
            }
        }
    }
}

[CollectionDefinition(Name)]
public sealed class AuthFixtureDefinition : ICollectionFixture<AuthFixture>
{
    public const string Name = "Auth";
}
