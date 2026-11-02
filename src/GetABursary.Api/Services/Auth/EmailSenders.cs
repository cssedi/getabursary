using System.Net;
using System.Net.Mail;
using GetABursary.Core.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GetABursary.Api.Services.Auth;

public sealed class SmtpOptions
{
    public const string SectionName = "Email:Smtp";

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public string User { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string From { get; init; } = string.Empty;
}

/// <summary>Turns Identity's API links into links to the web app and hands the message to <see cref="SendAsync"/>.</summary>
public abstract class LinkEmailSender(IConfiguration configuration) : IEmailSender<User>
{
    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        // Identity's link is <api>/confirmEmail?userId=..&code=..; the web page calls that endpoint.
        var query = new Uri(confirmationLink, UriKind.Absolute).Query;
        return SendAsync(email, "Confirm your GetABursary email", $"Confirm your email: {WebUrl("/account/confirm")}{query}");
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink) =>
        SendAsync(email, "Reset your GetABursary password", $"Reset your password: {resetLink}");

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode) =>
        SendAsync(
            email,
            "Reset your GetABursary password",
            $"Reset your password: {WebUrl("/account/reset")}?email={Uri.EscapeDataString(email)}&code={Uri.EscapeDataString(resetCode)}");

    protected abstract Task SendAsync(string recipient, string subject, string body);

    private string WebUrl(string path) => (configuration["App:PublicBaseUrl"] ?? "http://localhost:4200").TrimEnd('/') + path;
}

/// <summary>Development sender: writes the email to the log.</summary>
public sealed partial class LogEmailSender(IConfiguration configuration, ILogger<LogEmailSender> logger) : LinkEmailSender(configuration)
{
    protected override Task SendAsync(string recipient, string subject, string body)
    {
        LogEmail(recipient, subject, body);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email to {To} ({Subject}): {Body}")]
    private partial void LogEmail(string to, string subject, string body);
}

/// <summary>Production sender using <c>Email:Smtp:*</c>.</summary>
public sealed class SmtpEmailSender(IConfiguration configuration, IOptions<SmtpOptions> options) : LinkEmailSender(configuration)
{
    protected override async Task SendAsync(string recipient, string subject, string body)
    {
        var smtp = options.Value;
        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtp.User, smtp.Password),
        };
        using var message = new MailMessage(smtp.From, recipient, subject, body);
        await client.SendMailAsync(message);
    }
}
