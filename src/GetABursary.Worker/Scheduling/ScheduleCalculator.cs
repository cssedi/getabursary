using Cronos;
using GetABursary.Core.Common;
using Microsoft.Extensions.Options;

namespace GetABursary.Worker.Scheduling;

public interface IScheduleCalculator
{
    /// <summary>The next cron occurrence strictly after <paramref name="now"/>, or <c>null</c> if there is none.</summary>
    DateTimeOffset? GetNextOccurrence(DateTimeOffset now);

    /// <summary>Next occurrence after the injected clock's current time.</summary>
    DateTimeOffset? GetNextOccurrence();
}

public sealed class ScheduleCalculator : IScheduleCalculator
{
    private readonly CronExpression _expression;
    private readonly TimeZoneInfo _zone;
    private readonly IClock _clock;

    public ScheduleCalculator(IClock clock, IOptions<IngestionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _clock = clock;
        var value = options.Value;
        _expression = CronExpression.Parse(value.Cron);
        _zone = TimeZoneInfo.FindSystemTimeZoneById(value.TimeZone);
    }

    public DateTimeOffset? GetNextOccurrence(DateTimeOffset now) =>
        _expression.GetNextOccurrence(now.ToUniversalTime(), _zone, inclusive: false);

    public DateTimeOffset? GetNextOccurrence() => GetNextOccurrence(_clock.UtcNow);
}
