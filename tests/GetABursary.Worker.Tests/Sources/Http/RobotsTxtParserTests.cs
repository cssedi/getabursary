using GetABursary.Worker.Sources.Http;

namespace GetABursary.Worker.Tests.Sources.Http;

public class RobotsTxtParserTests
{
    private const string Token = "GetABursaryBot";

    [Fact]
    public void Parse_SpecificGroup_BeatsWildcardGroup()
    {
        var rules = RobotsTxtParser.Parse("User-agent: *\nDisallow: /\n\nUser-agent: GetABursaryBot\nDisallow: /private\n", Token);

        Assert.True(rules.IsAllowed("/bursaries"));
        Assert.False(rules.IsAllowed("/private/x"));
    }

    [Fact]
    public void Parse_NoSpecificGroup_FallsBackToWildcard()
    {
        var rules = RobotsTxtParser.Parse("User-agent: OtherBot\nDisallow: /\n\nUser-agent: *\nDisallow: /admin\n", Token);

        Assert.True(rules.IsAllowed("/open"));
        Assert.False(rules.IsAllowed("/admin/panel"));
    }

    [Fact]
    public void Parse_LongestMatchWins()
    {
        var rules = RobotsTxtParser.Parse("User-agent: *\nDisallow: /a\nAllow: /a/b\n", Token);

        Assert.True(rules.IsAllowed("/a/b/c"));
        Assert.False(rules.IsAllowed("/a/x"));
    }

    [Fact]
    public void Parse_AllowAndDisallowOfEqualLength_AllowWins()
    {
        var rules = RobotsTxtParser.Parse("User-agent: *\nDisallow: /page\nAllow: /page\n", Token);

        Assert.True(rules.IsAllowed("/page"));
    }

    [Fact]
    public void Parse_Wildcard_MatchesAnySequence()
    {
        var rules = RobotsTxtParser.Parse("User-agent: *\nDisallow: /*/private\n", Token);

        Assert.False(rules.IsAllowed("/x/y/private"));
        Assert.True(rules.IsAllowed("/x/public"));
    }

    [Fact]
    public void Parse_DollarAnchor_MatchesOnlyAtEnd()
    {
        var rules = RobotsTxtParser.Parse("User-agent: *\nDisallow: /*.pdf$\n", Token);

        Assert.False(rules.IsAllowed("/files/a.pdf"));
        Assert.True(rules.IsAllowed("/files/a.pdf?x=1"));
        Assert.True(rules.IsAllowed("/files/a.pdfx"));
    }

    [Fact]
    public void Parse_EmptyDisallow_AllowsEverything()
    {
        var rules = RobotsTxtParser.Parse("User-agent: *\nDisallow:\n", Token);

        Assert.True(rules.IsAllowed("/anything"));
    }

    [Fact]
    public void Parse_CrawlDelay_IsParsedAsSeconds()
    {
        var rules = RobotsTxtParser.Parse("User-agent: *\nCrawl-delay: 7.5\n", Token);

        Assert.Equal(TimeSpan.FromSeconds(7.5), rules.CrawlDelay);
    }

    [Fact]
    public void Parse_NoCrawlDelay_IsNull()
    {
        var rules = RobotsTxtParser.Parse("User-agent: *\nDisallow: /x\n", Token);

        Assert.Null(rules.CrawlDelay);
    }

    [Fact]
    public void Parse_CommentsAndBlankLines_AreIgnored()
    {
        var rules = RobotsTxtParser.Parse("# top comment\r\nUser-agent: * # everyone\r\n\r\nDisallow: /secret # hidden\r\n", Token);

        Assert.False(rules.IsAllowed("/secret/1"));
        Assert.True(rules.IsAllowed("/public"));
    }

    [Fact]
    public void Parse_ConsecutiveUserAgentLines_ShareOneGroup()
    {
        var rules = RobotsTxtParser.Parse("User-agent: OtherBot\nUser-agent: GetABursaryBot\nDisallow: /shared\n", Token);

        Assert.False(rules.IsAllowed("/shared"));
    }

    [Fact]
    public void Parse_UserAgentMatching_IsCaseInsensitive()
    {
        var rules = RobotsTxtParser.Parse("User-agent: getabursarybot\nDisallow: /x\n", Token);

        Assert.False(rules.IsAllowed("/x"));
    }

    [Fact]
    public void Parse_EmptyFile_AllowsEverything()
    {
        var rules = RobotsTxtParser.Parse(string.Empty, Token);

        Assert.True(rules.IsAllowed("/anything"));
    }
}
