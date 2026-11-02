using GetABursary.Core.Common;

namespace GetABursary.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers cross-cutting services shared by every feature (currently <see cref="IClock"/>).</summary>
    public static IServiceCollection AddCommon(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
