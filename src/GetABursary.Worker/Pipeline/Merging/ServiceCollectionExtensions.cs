using GetABursary.Core.Repositories.Bursaries;
using GetABursary.Core.Repositories.Reports;

namespace GetABursary.Worker.Pipeline.Merging;

public static class MergingServiceCollectionExtensions
{
    public static IServiceCollection AddBursaryMerging(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IBursaryWriteRepository, BursaryWriteRepository>();
        services.AddScoped<IFunderRepository, FunderRepository>();
        services.AddScoped<IReviewItemRepository, ReviewItemRepository>();
        services.AddScoped<IBursaryMerger, BursaryMerger>();
        return services;
    }
}
