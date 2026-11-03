using GetABursary.Api.Extensions;
using GetABursary.Api.Services.Auth;
using GetABursary.Core.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GetABursary.Api.Tests.Auth;

public sealed class EmailSenderSelectionTests
{
    [Theory]
    [InlineData("Development", "smtp.example.test", typeof(LogEmailSender))]
    [InlineData("Production", "", typeof(LogEmailSender))]
    [InlineData("Production", "smtp.example.test", typeof(SmtpEmailSender))]
    public void AddAuthFeature_PicksSenderFromEnvironmentAndSmtpHost(string environment, string smtpHost, Type expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Smtp:Host"] = smtpHost,
                ["Email:Smtp:From"] = "no-reply@example.test",
            })
            .Build();
        var services = new ServiceCollection();

        services.AddAuthFeature(configuration, new TestHostEnvironment(environment));

        var sender = services.Last(d => d.ServiceType == typeof(IEmailSender<User>));
        Assert.Equal(expected, sender.ImplementationType);
    }

    [Theory]
    [InlineData("Development", "", true)]
    [InlineData("Production", "", false)]
    [InlineData("Production", "smtp.example.test", true)]
    public void AddAuthFeature_RequiresConfirmedEmailOnlyWhenMailCanBeSent(string environment, string smtpHost, bool expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Email:Smtp:Host"] = smtpHost })
            .Build();
        var services = new ServiceCollection();
        services.AddAuthFeature(configuration, new TestHostEnvironment(environment));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        Assert.Equal(expected, options.SignIn.RequireConfirmedEmail);
    }

    private sealed class TestHostEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "GetABursary.Api";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
