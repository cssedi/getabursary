using GetABursary.Core.Repositories.Ingestion;

namespace GetABursary.Worker.Pipeline.Jobs;

public static class IngestionPipelineServiceCollectionExtensions
{
    /// <summary>Registers the ingestion pipeline and its closure and vanished jobs.</summary>
    public static IServiceCollection AddIngestionPipeline(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IIngestionSourceRepository, IngestionSourceRepository>();
        services.AddScoped<IClosureJob, ClosureJob>();
        services.AddScoped<IVanishedJob, VanishedJob>();
        services.AddScoped<IIngestionPipeline, IngestionPipeline>();
        return services;
    }
}
