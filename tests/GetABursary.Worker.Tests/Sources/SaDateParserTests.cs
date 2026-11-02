using GetABursary.Worker.Sources.Parsing;

namespace GetABursary.Worker.Tests.Sources;

public class SaDateParserTests
{
    [Theory]
    [InlineData("31 March 2027")]
    [InlineData("31 Mar 2027")]
    [InlineData("31/03/2027")]
    [InlineData("2027-03-31")]
    [InlineData("March 31, 2027")]
    [InlineData("Closing date: 31 March 2027")]
    [InlineData("Applications close on 31st March 2027 at 16:00")]
    public void TryParse_SupportedFormat_ReturnsDate(string text)
    {
        Assert.True(SaDateParser.TryParse(text, out var date));
        Assert.Equal(new DateOnly(2027, 3, 31), date);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("not a date")]
    [InlineData("31 Smarch 2027")]
    [InlineData("32/13/2027")]
    [InlineData("Open all year")]
    public void TryParse_InvalidInput_ReturnsFalse(string? text)
    {
        Assert.False(SaDateParser.TryParse(text, out var date));
        Assert.Equal(default, date);
    }
}
