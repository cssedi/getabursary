using System.ComponentModel.DataAnnotations;

namespace GetABursary.Api.Services.Reports;

/// <summary>Bound from the <c>Reports</c> configuration section.</summary>
public sealed class ReportsOptions
{
    public const string SectionName = "Reports";

    /// <summary>
    /// Secret salt for <c>ip_hash</c>. Must be set outside Development (e.g. the <c>Reports__IpHashSalt</c>
    /// environment variable). Never commit a real value.
    /// </summary>
    public string IpHashSalt { get; set; } = string.Empty;

    /// <summary>Maximum anonymous reports per IP address per hour.</summary>
    [Range(1, int.MaxValue)]
    public int RateLimitPerHour { get; set; } = 5;
}
