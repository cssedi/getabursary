using GetABursary.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GetABursary.Core.Data;

/// <summary>Lets <c>dotnet ef</c> create the context. Uses <c>ConnectionStrings__Default</c> or the local docker default.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<GetABursaryDbContext>
{
    private const string LocalDefault = "Host=localhost;Port=5433;Database=getabursary;Username=getabursary;Password=localdev";

    public GetABursaryDbContext CreateDbContext(string[] args)
    {
        var fromEnv = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        var connectionString = string.IsNullOrWhiteSpace(fromEnv) ? LocalDefault : fromEnv;

        var options = new DbContextOptionsBuilder<GetABursaryDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new GetABursaryDbContext(options, new SystemClock());
    }
}
