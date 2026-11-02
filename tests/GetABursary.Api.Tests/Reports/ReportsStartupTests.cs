using GetABursary.Api.Services.Reports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace GetABursary.Api.Tests.Reports;

public class ReportsStartupTests
{
    [Fact]
    public void Startup_ProductionWithEmptyIpHashSalt_FailsFast()
    {
        using var factory = ProductionFactory(salt: string.Empty);

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }

    [Fact]
    public void Startup_ProductionWithIpHashSalt_Starts()
    {
        using var factory = ProductionFactory(salt: "production-salt-from-environment");

        using var client = factory.CreateClient();

        Assert.NotNull(client);
    }

    private static WebApplicationFactory<Program> ProductionFactory(string salt) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("Reports:IpHashSalt", salt);
            builder.UseSetting("Email:Smtp:Host", "smtp.example.test");
            builder.UseSetting("Email:Smtp:From", "noreply@example.test");
        });
}
