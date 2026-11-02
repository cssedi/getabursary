using GetABursary.Api.Services.Tracker;
using GetABursary.Core.Repositories.Tracker;

namespace GetABursary.Api.Extensions;

public static class TrackerServiceCollectionExtensions
{
    /// <summary>Registers the saved-bursaries and application-tracker feature (FR-12–FR-14): repository and service.</summary>
    public static IServiceCollection AddTrackerFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ITrackerRepository, TrackerRepository>();
        services.AddScoped<ITrackerService, TrackerService>();

        return services;
    }
}
