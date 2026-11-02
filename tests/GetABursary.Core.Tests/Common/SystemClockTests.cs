using GetABursary.Core.Common;

namespace GetABursary.Core.Tests.Common;

public class SystemClockTests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Fact]
    public void UtcNow_ReturnsTimeFromTimeProvider()
    {
        var instant = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
        var clock = new SystemClock(new FixedTimeProvider(instant));

        Assert.Equal(instant, clock.UtcNow);
    }

    [Fact]
    public void Today_UsesSastDate_WhenUtcDateIsStillPrevious()
    {
        // 23:30 UTC on 1 Nov is 01:30 SAST (UTC+2) on 2 Nov.
        var instant = new DateTimeOffset(2026, 11, 1, 23, 30, 0, TimeSpan.Zero);
        var clock = new SystemClock(new FixedTimeProvider(instant));

        Assert.Equal(new DateOnly(2026, 11, 2), clock.Today);
    }

    [Fact]
    public void Today_UsesSastDate_WhenUtcDateIsAlreadyNext()
    {
        // 23:30 UTC on 2 Nov is 01:30 SAST on 3 Nov, so SAST is already a day ahead of the UTC date.
        var instant = new DateTimeOffset(2026, 11, 2, 23, 30, 0, TimeSpan.Zero);
        var clock = new SystemClock(new FixedTimeProvider(instant));

        Assert.Equal(new DateOnly(2026, 11, 3), clock.Today);
    }
}
