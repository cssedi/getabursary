using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Core.Tests.Data.Bursaries;

public class BursaryDomainTests
{
    private static readonly DateOnly Today = new(2026, 6, 1);

    private static Bursary Make(BursaryStatus status, DateOnly? closing) => new()
    {
        Slug = "s",
        Name = "n",
        NormalizedName = "n",
        Status = status,
        ClosingDate = closing,
    };

    [Fact]
    public void IsClosingSoon_OpenAndClosesIn14Days_ReturnsTrue() =>
        Assert.True(Make(BursaryStatus.Open, Today.AddDays(14)).IsClosingSoon(Today));

    [Fact]
    public void IsClosingSoon_OpenAndClosesToday_ReturnsTrue() =>
        Assert.True(Make(BursaryStatus.Open, Today).IsClosingSoon(Today));

    [Fact]
    public void IsClosingSoon_OpenAndClosesIn15Days_ReturnsFalse() =>
        Assert.False(Make(BursaryStatus.Open, Today.AddDays(15)).IsClosingSoon(Today));

    [Fact]
    public void IsClosingSoon_OpenAndAlreadyPastClosingDate_ReturnsFalse() =>
        Assert.False(Make(BursaryStatus.Open, Today.AddDays(-1)).IsClosingSoon(Today));

    [Fact]
    public void IsClosingSoon_Closed_ReturnsFalse() =>
        Assert.False(Make(BursaryStatus.Closed, Today.AddDays(3)).IsClosingSoon(Today));

    [Fact]
    public void IsClosingSoon_NullClosingDate_ReturnsFalse() =>
        Assert.False(Make(BursaryStatus.Open, null).IsClosingSoon(Today));

    [Fact]
    public void IsLocked_FieldInLockedFields_ReturnsTrue()
    {
        var bursary = Make(BursaryStatus.Open, null);
        bursary.LockedFields.Add("closing_date");
        Assert.True(bursary.IsLocked("closing_date"));
    }

    [Fact]
    public void IsLocked_FieldNotInLockedFields_ReturnsFalse()
    {
        var bursary = Make(BursaryStatus.Open, null);
        bursary.LockedFields.Add("closing_date");
        Assert.False(bursary.IsLocked("summary"));
    }
}
