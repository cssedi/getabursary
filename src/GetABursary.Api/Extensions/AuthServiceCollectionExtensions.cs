using GetABursary.Api.Services.Auth;
using GetABursary.Core.Data;
using GetABursary.Core.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace GetABursary.Api.Extensions;

public static class AuthServiceCollectionExtensions
{
    public const string CsrfHeader = "X-Requested-With";

    public const string CsrfHeaderValue = "getabursary";

    /// <summary>Identity with cookie auth (ADR-003): roles, confirmed email, 401/403 instead of redirects, and the email sender.</summary>
    public static IServiceCollection AddAuthFeature(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddIdentityApiEndpoints<User>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddUserManager<AppUserManager>()
            .AddEntityFrameworkStores<GetABursaryDbContext>();

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

        services.AddOptions<SmtpOptions>().Bind(configuration.GetSection(SmtpOptions.SectionName));
        if (environment.IsDevelopment())
        {
            services.AddSingleton<IEmailSender<User>, LogEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender<User>, SmtpEmailSender>();
        }

        return services;
    }

    /// <summary>Maps Identity's endpoints under <c>/api/v1/auth</c>, plus <c>logout</c> (which MapIdentityApi does not provide).</summary>
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");
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
