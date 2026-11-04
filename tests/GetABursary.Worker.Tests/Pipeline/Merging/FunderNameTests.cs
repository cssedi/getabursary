using GetABursary.Worker.Pipeline.Merging;

namespace GetABursary.Worker.Tests.Pipeline.Merging;

public class FunderNameTests
{
    [Theory]
    [InlineData("SRK Consulting (South Africa) (administered in-house; [email protected])", "SRK Consulting (South Africa)")]
    [InlineData("Exxaro Resources Limited (applications via the Exxaro eRecruit portal)", "Exxaro Resources Limited")]
    [InlineData(
        "South African Institution of Civil Engineering (SAICE) — administered in-house via the SAICE Patrons' Engineering Bursary Scheme (SPEBS) desk",
        "South African Institution of Civil Engineering (SAICE)")]
    [InlineData("Acme Foundation ([email&#160;protected])", "Acme Foundation")]
    [InlineData("MTN Group", "MTN Group")]
    [InlineData("Auditor-General of South Africa (AGSA)", "Auditor-General of South Africa (AGSA)")]
    [InlineData("FASSET", "FASSET")]
    [InlineData("BBD", "BBD")]
    [InlineData("Sasol (Pty) Ltd", "Sasol (Pty) Ltd")]
    public void Clean_ScrapedOrNormalName_ReturnsExpected(string scraped, string expected)
    {
        Assert.Equal(expected, FunderName.Clean(scraped));
    }

    [Fact]
    public void Clean_OnlyAdminNote_FallsBackToTrimmedOriginal()
    {
        Assert.Equal("(administered in-house)", FunderName.Clean("  (administered in-house)  "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Clean_BlankInput_ReturnsNull(string? scraped)
    {
        Assert.Null(FunderName.Clean(scraped));
    }
}
