using GetABursary.Core.Repositories.Ingestion;
using GetABursary.Worker.Pipeline;
using Microsoft.Extensions.Configuration;

namespace GetABursary.Worker.Scheduling;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIngestionScheduling(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IngestionOptions>(configuration.GetSection(IngestionOptions.SectionName));
        services.AddScoped<IIngestionRunRepository, IngestionRunRepository>();
        services.AddScoped<IIngestionPipeline, NoOpIngestionPipeline>();
        services.AddSingleton<IScheduleCalculator, ScheduleCalculator>();
        services.AddSingleton<IRunTrigger, RunTrigger>();
        services.AddHostedService<IngestionScheduler>();
        return services;
    }
}
