using GetABursary.Api.Services.Taxonomy;
using GetABursary.Core.Repositories.Taxonomy;

namespace GetABursary.Api.Extensions;

public static class TaxonomyServiceCollectionExtensions
{
    /// <summary>Registers the read-only taxonomy feature (FR-01): repository and service.</summary>
    public static IServiceCollection AddTaxonomyFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ITaxonomyRepository, TaxonomyRepository>();
        services.AddScoped<ITaxonomyService, TaxonomyService>();

        return services;
    }
}
