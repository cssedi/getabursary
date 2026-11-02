using GetABursary.Core.Domain.Bursaries;

namespace GetABursary.Core.Tests.Domain.Bursaries;

public class NameNormalizerTests
{
    [Theory]
    [InlineData("Sasol Bursary 2026", "sasol")]
    [InlineData("Sasol Bursary 2026/2027", "sasol")]
    [InlineData("Funza Lushaka Bursary Programme", "funza lushaka")]
    [InlineData("NSFAS Bursaries Scheme", "nsfas")]
    [InlineData("Allan Gray Scholarship Program", "allan gray")]
    [InlineData("Engineering Bursary 12026", "engineering 12026")]
    public void Normalize_RemovesYearsAndStopWords(string input, string expected) =>
        Assert.Equal(expected, NameNormalizer.Normalize(input));

    [Theory]
    [InlineData("Eskom (Pty) Ltd. – Engineering!", "eskom pty ltd engineering")]
    [InlineData("Anglo American/De Beers", "anglo american de beers")]
    [InlineData("Nkosi's Fund", "nkosis fund")]
    public void Normalize_RemovesPunctuation(string input, string expected) =>
        Assert.Equal(expected, NameNormalizer.Normalize(input));

    [Fact]
    public void Normalize_MixedCaseAndRepeatedWhitespace_IsLowercasedAndCollapsed() =>
        Assert.Equal("old mutual", NameNormalizer.Normalize("  OLD   Mutual \t BURSARY  "));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bursary 2026")]
    public void Normalize_NothingLeft_ReturnsEmpty(string? input) =>
        Assert.Equal(string.Empty, NameNormalizer.Normalize(input));

    [Fact]
    public void Normalize_StopWordInsideWord_IsKept() =>
        Assert.Equal("programmer academy", NameNormalizer.Normalize("Programmer Academy"));
}
