using GetABursary.Api.Services.Admin;
using GetABursary.Core.Repositories.Admin;
using GetABursary.Core.Repositories.Bursaries;
using GetABursary.Core.Repositories.Ingestion;

namespace GetABursary.Api.Extensions;

public static class AdminServiceCollectionExtensions
{
    /// <summary>Registers the admin review queue and bursary editor (FR-40–FR-42).</summary>
    public static IServiceCollection AddAdminFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IBursaryWriteRepository, BursaryWriteRepository>();
        services.AddScoped<IFunderRepository, FunderRepository>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IIngestionSourceRepository, IngestionSourceRepository>();
        services.AddScoped<IIngestionRunRepository, IngestionRunRepository>();
        services.AddScoped<IAdminSourceService, AdminSourceService>();

        return services;
    }
}
