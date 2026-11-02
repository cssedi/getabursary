using GetABursary.Core.Common;
using GetABursary.Worker.Scheduling;
using Microsoft.Extensions.Options;

namespace GetABursary.Worker.Tests.Scheduling;

public class ScheduleCalculatorTests
{
    private static ScheduleCalculator Create(DateTimeOffset now, string cron = "0 3 * * *") =>
        new(new FixedClock(now), Options.Create(new IngestionOptions { Cron = cron }));

    [Fact]
    public void GetNextOccurrence_BeforeThreeAmSast_ReturnsSameDayThreeAm()
    {
        var now = new DateTimeOffset(2026, 3, 10, 0, 30, 0, TimeSpan.Zero); // 02:30 SAST
        var next = Create(now).GetNextOccurrence(now);

        Assert.Equal(new DateTimeOffset(2026, 3, 10, 3, 0, 0, TimeSpan.FromHours(2)), next);
    }

    [Fact]
    public void GetNextOccurrence_AfterThreeAmSast_ReturnsNextDay()
    {
        var now = new DateTimeOffset(2026, 3, 10, 1, 30, 0, TimeSpan.Zero); // 03:30 SAST
        var next = Create(now).GetNextOccurrence(now);

        Assert.Equal(new DateTimeOffset(2026, 3, 11, 3, 0, 0, TimeSpan.FromHours(2)), next);
    }

    [Fact]
    public void GetNextOccurrence_ExactlyAtThreeAm_ReturnsNextDay()
    {
        var now = new DateTimeOffset(2026, 3, 10, 3, 0, 0, TimeSpan.FromHours(2));
        var next = Create(now).GetNextOccurrence(now);

        Assert.Equal(new DateTimeOffset(2026, 3, 11, 3, 0, 0, TimeSpan.FromHours(2)), next);
    }

    [Fact]
    public void GetNextOccurrence_UsesInjectedClock()
    {
        var now = new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
        var next = Create(now).GetNextOccurrence();

        Assert.Equal(new DateTimeOffset(2026, 3, 11, 1, 0, 0, TimeSpan.Zero), next);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;

        public DateOnly Today => DateOnly.FromDateTime(now.UtcDateTime);
    }
}
