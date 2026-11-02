using GetABursary.Core.Common;
using GetABursary.Core.Data;
using GetABursary.Core.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace GetABursary.Core;

public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="GetABursaryDbContext"/> (PostgreSQL, snake_case), <see cref="IClock"/> if missing,
    /// and <see cref="IDataSeeder"/>. The seed folder comes from <c>Seeding:Path</c>.
    /// </summary>
    public static IServiceCollection AddCoreData(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton<IClock, SystemClock>();
        services.AddDbContext<GetABursaryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default"))
                .UseSnakeCaseNamingConvention());
        services.AddScoped<IDataSeeder>(sp => new DataSeeder(
            sp.GetRequiredService<GetABursaryDbContext>(),
            DataSeeder.ResolveSeedDirectory(configuration["Seeding:Path"]),
            sp.GetRequiredService<ILogger<DataSeeder>>()));

        return services;
    }
}
