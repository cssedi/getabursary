namespace GetABursary.Core.Common;

/// <summary>Production <see cref="IClock"/> backed by a <see cref="TimeProvider"/> (system clock by default).</summary>
public sealed class SystemClock(TimeProvider? timeProvider = null) : IClock
{
    private const string SouthAfricaTimeZoneId = "Africa/Johannesburg";

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public DateOnly Today
    {
        get
        {
            var sast = TimeZoneInfo.FindSystemTimeZoneById(SouthAfricaTimeZoneId);
            var local = TimeZoneInfo.ConvertTime(UtcNow, sast);
            return DateOnly.FromDateTime(local.DateTime);
        }
    }
}
