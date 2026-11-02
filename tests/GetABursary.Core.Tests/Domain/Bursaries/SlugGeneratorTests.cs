using GetABursary.Core.Domain.Bursaries;

namespace GetABursary.Core.Tests.Domain.Bursaries;

public class SlugGeneratorTests
{
    private static readonly Guid Id = Guid.Parse("0123abcd-0000-7000-8000-000000000000");

    [Fact]
    public void Create_AppendsFirstSixHexCharsOfId() =>
        Assert.Equal("sasol-engineering-0123ab", SlugGenerator.Create("Sasol Engineering", Id));

    [Theory]
    [InlineData("Eskom (Pty) Ltd. – Engineering!", "eskom-pty-ltd-engineering-0123ab")]
    [InlineData("  --Funza   Lushaka-- ", "funza-lushaka-0123ab")]
    [InlineData("Café Fund", "cafe-fund-0123ab")]
    [InlineData("Nkosi's Fund", "nkosis-fund-0123ab")]
    public void Create_ProducesKebabCase(string name, string expected) =>
        Assert.Equal(expected, SlugGenerator.Create(name, Id));

    [Fact]
    public void Create_NoUsableCharacters_UsesIdPrefixOnly() =>
        Assert.Equal("0123ab", SlugGenerator.Create("!!!", Id));

    [Fact]
    public void Create_VeryLongName_IsTruncated() =>
        Assert.True(SlugGenerator.Create(new string('a', 500), Id).Length <= 200);
}
