using System.Globalization;
using System.Threading.RateLimiting;
using GetABursary.Api.Services.Reports;
using GetABursary.Core.Repositories.Reports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace GetABursary.Api.Extensions;

/// <summary>Rate limiter policy used by the anonymous report endpoint.</summary>
public static class ReportsRateLimiting
{
    public const string PolicyName = "reports";
}

public static class ReportsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the report-an-inaccuracy feature (FR-08): options, repository, service and the
    /// <c>reports</c> rate-limit policy. Fails at startup outside Development when the IP hash salt is empty.
    /// </summary>
    public static IServiceCollection AddReportsFeature(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddOptions<ReportsOptions>()
            .Bind(configuration.GetSection(ReportsOptions.SectionName))
            .Validate(
                options => environment.IsDevelopment() || !string.IsNullOrWhiteSpace(options.IpHashSalt),
                "Reports:IpHashSalt must be set outside Development (use the Reports__IpHashSalt environment variable).")
            .Validate(options => options.RateLimitPerHour >= 1, "Reports:RateLimitPerHour must be at least 1.")
            .ValidateOnStart();

        services.AddScoped<IListingReportRepository, ListingReportRepository>();
        services.AddScoped<IReportService, ReportService>();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.AddPolicy<string>(ReportsRateLimiting.PolicyName, context =>
            {
                var perHour = context.RequestServices.GetRequiredService<IOptions<ReportsOptions>>().Value.RateLimitPerHour;

                return RateLimitPartition.GetFixedWindowLimiter(
                    ClientIp.From(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = perHour,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    });
            });

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                await Results.Problem(
                    title: "Too many reports",
                    detail: "You have reached the limit for reporting inaccuracies. Please try again later.",
                    statusCode: StatusCodes.Status429TooManyRequests)
                    .ExecuteAsync(context.HttpContext);
            };
        });

        return services;
    }
}
