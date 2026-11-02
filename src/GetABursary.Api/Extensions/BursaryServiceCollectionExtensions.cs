using GetABursary.Api.Services.Bursaries;
using GetABursary.Core.Repositories.Bursaries;
using GetABursary.Core.Repositories.Taxonomy;

namespace GetABursary.Api.Extensions;

public static class BursaryServiceCollectionExtensions
{
    /// <summary>Registers bursary search (FR-01–FR-03) and bursary detail (FR-06, FR-07): repositories and services.</summary>
    public static IServiceCollection AddBursaryFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IBursaryReadRepository, BursaryReadRepository>();
        services.AddScoped<IQualificationResolutionRepository, QualificationResolutionRepository>();
        services.AddScoped<IBursaryMatchingService, BursaryMatchingService>();
        services.AddScoped<IBursaryService, BursaryService>();

        return services;
    }
}
