using System.Threading.RateLimiting;
using GetABursary.Api.Services.Auth;
using GetABursary.Api.Services.Reports;
using GetABursary.Core.Data;
using GetABursary.Core.Domain.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace GetABursary.Api.Extensions;

public static class AuthServiceCollectionExtensions
{
    public const string CsrfHeader = "X-Requested-With";

    public const string AuthRateLimitPolicy = "auth";

    public const string CsrfHeaderValue = "getabursary";

    /// <summary>Identity with cookie auth (ADR-003): roles, confirmed email, 401/403 instead of redirects, and the email sender.</summary>
    public static IServiceCollection AddAuthFeature(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        // Keys live in Postgres so sign-in cookies survive restarts and work across instances (spec 08 §2c).
        // shortcut: keys are stored unencrypted; add ProtectKeysWithCertificate if the DB is shared or backups leave our control.
        services.AddDataProtection()
            .SetApplicationName("getabursary")
            .PersistKeysToDbContext<GetABursaryDbContext>();

        services.AddIdentityApiEndpoints<User>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddUserManager<AppUserManager>()
            .AddEntityFrameworkStores<GetABursaryDbContext>();

        services.AddScoped<IExternalLoginService, ExternalLoginService>();

        var clientId = configuration["Auth:Google:ClientId"];
        var clientSecret = configuration["Auth:Google:ClientSecret"];
        if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
        {
            services.AddAuthentication().AddGoogle(options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.ClaimActions.MapJsonKey("email_verified", "email_verified");
            });
        }

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddRateLimiter(limiter => limiter.AddPolicy<string>(AuthRateLimitPolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                ClientIp.From(context),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = configuration.GetValue("Auth:RateLimitPerHour", 60), Window = TimeSpan.FromHours(1), QueueLimit = 0 })));

        // shortcut: with no SMTP host set (test server, mail not set up yet), emails go to the log, so the owner can
        // copy confirm/reset links from `docker compose logs api`. Set Email:Smtp:Host before real users sign up.
        if (environment.IsDevelopment() || string.IsNullOrWhiteSpace(configuration[$"{SmtpOptions.SectionName}:Host"]))
        {
            services.AddSingleton<IEmailSender<User>, LogEmailSender>();
        }
        else
        {
            services.AddOptions<SmtpOptions>()
                .Bind(configuration.GetSection(SmtpOptions.SectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.Host) && !string.IsNullOrWhiteSpace(o.From), "Email:Smtp:Host and Email:Smtp:From are required.")
                .ValidateOnStart();
            services.AddSingleton<IEmailSender<User>, SmtpEmailSender>();
        }

        return services;
    }

    /// <summary>Maps Identity's endpoints under <c>/api/v1/auth</c>, plus <c>logout</c> (which MapIdentityApi does not provide).</summary>
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").RequireRateLimiting(AuthRateLimitPolicy);
        group.MapIdentityApi<User>();
        group.MapPost("/logout", async (SignInManager<User> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok();
        });
    }

    /// <summary>Rejects state-changing /api/v1 requests that lack the <c>X-Requested-With: getabursary</c> header (ADR-003).</summary>
    public static IApplicationBuilder UseCsrfHeaderCheck(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var request = context.Request;
            if (request.Path.StartsWithSegments("/api/v1", StringComparison.OrdinalIgnoreCase)
                && !HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) && !HttpMethods.IsOptions(request.Method)
                && request.Headers[CsrfHeader] != CsrfHeaderValue)
            {
                await Results.Problem(
                    title: "Missing header",
                    detail: $"Requests that change data must send the header '{CsrfHeader}: {CsrfHeaderValue}'.",
                    statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(context);
                return;
            }

            await next();
        });
}
