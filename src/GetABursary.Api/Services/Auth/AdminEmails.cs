namespace GetABursary.Api.Services.Auth;

/// <summary>Reads the comma-separated <c>Admin:Emails</c> setting.</summary>
public static class AdminEmails
{
    public static string[] From(IConfiguration configuration) =>
        (configuration["Admin:Emails"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static bool Contains(IConfiguration configuration, string? email) =>
        email is not null && From(configuration).Contains(email, StringComparer.OrdinalIgnoreCase);
}
