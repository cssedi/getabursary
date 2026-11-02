using GetABursary.Core.Repositories.Taxonomy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GetABursary.Worker.Extraction;

public static class ExtractionServiceCollectionExtensions
{
    /// <summary>Registers the extractor chosen by <c>Extraction:Provider</c> ("anthropic" or the default "fake").</summary>
    public static IServiceCollection AddEligibilityExtraction(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ExtractionOptions>(configuration.GetSection(ExtractionOptions.SectionName));
        services.TryAddScoped<IFieldOfStudyRepository, FieldOfStudyRepository>();

        var provider = configuration[$"{ExtractionOptions.SectionName}:Provider"];
        if (string.Equals(provider, "anthropic", StringComparison.OrdinalIgnoreCase))
        {
            services.TryAddSingleton<IFieldCatalog, FieldCatalog>();
            services.AddHttpClient<IEligibilityExtractor, LlmEligibilityExtractor>(c => c.Timeout = TimeSpan.FromSeconds(60));
        }
        else
        {
            services.TryAddSingleton<IEligibilityExtractor, FakeEligibilityExtractor>();
        }

        return services;
    }
}
