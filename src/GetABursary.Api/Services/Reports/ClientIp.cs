using System.Net;
using Microsoft.AspNetCore.Http;

namespace GetABursary.Api.Services.Reports;

/// <summary>Resolves the caller's IP for report hashing and rate-limit partitioning.</summary>
public static class ClientIp
{
    /// <summary>Used when the connection has no remote address (for example, some in-memory test hosts).</summary>
    public const string Unknown = "unknown";

    public static string From(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var address = context.Connection.RemoteIpAddress;
        if (address is null)
        {
            return Unknown;
        }

        // Treat ::ffff:1.2.3.4 and 1.2.3.4 as the same client.
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
    }
}
