using System.Net;
using GetABursary.Core.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace GetABursary.Worker.Sources.Http;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPoliteHttpClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddSingleton<IDelayer, TaskDelayer>();
        services.TryAddSingleton<PoliteHostState>();

        services.AddHttpClient<IPageFetcher, PoliteHttpClient>()
            .AddStandardResilienceHandler(options =>
            {
                // 3 attempts in total = the first try plus 2 retries.
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.Delay = TimeSpan.FromSeconds(2);
                options.Retry.ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is HttpRequestException or TimeoutException
                    || args.Outcome.Result is { StatusCode: HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests }
                    || args.Outcome.Result is { StatusCode: >= HttpStatusCode.InternalServerError });
            });

        return services;
    }
}
