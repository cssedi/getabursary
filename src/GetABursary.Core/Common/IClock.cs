namespace GetABursary.Core.Common;

/// <summary>Source of the current time. Inject this instead of reading <c>DateTime.Now</c>/<c>UtcNow</c> so tests can control time.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>The current calendar date in South African Standard Time (Africa/Johannesburg).</summary>
    DateOnly Today { get; }
}
