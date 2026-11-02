using GetABursary.Core.Common;
using GetABursary.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GetABursary.Core;

public static class DependencyInjection
{
    /// <summary>Registers <see cref="GetABursaryDbContext"/> (PostgreSQL, snake_case) and <see cref="IClock"/> if missing.</summary>
    public static IServiceCollection AddCoreData(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton<IClock, SystemClock>();
        services.AddDbContext<GetABursaryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default"))
                .UseSnakeCaseNamingConvention());

        return services;
    }
}
