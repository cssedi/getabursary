using GetABursary.Core.Common;
using GetABursary.Worker.Sources;
using GetABursary.Worker.Sources.Adapters.ZaBursaries;
using GetABursary.Worker.Sources.Http;

namespace GetABursary.Worker.Tests.Sources.Adapters.ZaBursaries;

public class ZaBursariesAdapterTests
{
    private static readonly Uri BaseUrl = new("https://www.zabursaries.co.za/");
    private static readonly SourceOptions Options = new(BaseUrl, 0);
    private readonly ZaBursariesAdapter _adapter = new();

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "zabursaries", name));

    private static Uri Detail(string path) => new(BaseUrl, path);

    private sealed class FakeFetcher(Dictionary<Uri, string> pages) : IPageFetcher
    {
        public List<Uri> Requested { get; } = [];

        public Task<Result<FetchedPage>> GetAsync(Uri url, int minDelayMs, CancellationToken ct)
        {
            Requested.Add(url);
            Result<FetchedPage> result = pages.TryGetValue(url, out var html)
                ? new FetchedPage(url, 200, html, DateTimeOffset.UnixEpoch)
                : FetchErrors.NotFound(url);
            return Task.FromResult(result);
        }
    }

    private async Task<List<Uri>> DiscoverAsync(FakeFetcher fetcher, SourceOptions options)
    {
        var urls = new List<Uri>();
        await foreach (var url in _adapter.DiscoverAsync(fetcher, options, CancellationToken.None))
        {
            urls.Add(url);
        }

        return urls;
    }

    [Fact]
    public async Task Discover_FromIndexFixture_ReturnsDetailUrls()
    {
        var fetcher = new FakeFetcher(new() { [BaseUrl] = Fixture("index-1.html") });

        var urls = await DiscoverAsync(fetcher, Options);

        // 7 distinct listings: duplicates, news, categories, guides and other hosts are ignored.
        Assert.Equal(7, urls.Count);
        Assert.Contains(Detail("accounting-bursaries-south-africa/fasset-bursary/"), urls);
        Assert.Contains(Detail("computer-science-it-bursaries-south-africa/amazon-bursary/"), urls);
        Assert.DoesNotContain(urls, u => u.AbsolutePath.Contains("bursary-news", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Discover_RespectsMaxListings()
    {
        var fetcher = new FakeFetcher(new() { [BaseUrl] = Fixture("index-1.html") });

        var urls = await DiscoverAsync(fetcher, Options with { MaxListings = 2 });

        Assert.Equal(2, urls.Count);
    }

    [Fact]
    public async Task Discover_FollowsNextPageUpToMaxPages()
    {
        var page2 = new Uri(BaseUrl, "page/2/");
        var fetcher = new FakeFetcher(new()
        {
            [BaseUrl] = "<a href='/law-bursaries-south-africa/one/'>1</a><a class='next' href='/page/2/'>next</a>",
            [page2] = "<a href='/law-bursaries-south-africa/two/'>2</a><a class='next' href='/page/3/'>next</a>",
        });

        var urls = await DiscoverAsync(fetcher, Options with { MaxPages = 2 });

        Assert.Equal([Detail("law-bursaries-south-africa/one/"), Detail("law-bursaries-south-africa/two/")], urls);
        Assert.Equal([BaseUrl, page2], fetcher.Requested);
    }

    [Fact]
    public void Parse_FassetFixture_ReadsH3Layout()
    {
        var url = Detail("accounting-bursaries-south-africa/fasset-bursary/");

        var result = _adapter.Parse(url, Fixture("detail-fasset-bursary.html"));

        Assert.True(result.IsSuccess);
        var listing = result.Value;
        Assert.Equal("FASSET Bursary South Africa 2026", listing.Title);
        Assert.Equal("FASSET", listing.FunderName);
        Assert.Equal(new DateOnly(2026, 2, 28), listing.ClosingDate);
        Assert.Null(listing.OpenDate);
        Assert.Equal(new Uri("https://forms.office.com/pages/responsepage.aspx?id=EXAMPLE&route=shorturl"), listing.ApplyUrl);
        Assert.Contains("minimum overall average of 75% in Matric", listing.EligibilityText, StringComparison.Ordinal);
        Assert.Contains("Actuarial Sciences", listing.FieldsHint, StringComparison.Ordinal);
        Assert.Contains("tuition fees", listing.ValueText, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_SarbFixture_ReadsH2Layout()
    {
        var url = Detail("commerce-bursaries-south-africa/sarb-economics-bursary/");

        var result = _adapter.Parse(url, Fixture("detail-sarb-economics-bursary.html"));

        Assert.True(result.IsSuccess);
        var listing = result.Value;
        Assert.Equal("SARB Economics Bursary South Africa 2027", listing.Title);
        Assert.Null(listing.FunderName);
        Assert.Equal(new DateOnly(2026, 9, 30), listing.ClosingDate);
        Assert.Equal(new Uri("https://sarb-bursary-manager.replit.app/"), listing.ApplyUrl);
        Assert.Contains("under 30 years old", listing.EligibilityText, StringComparison.Ordinal);
        Assert.Contains("Economics and Econometrics", listing.FieldsHint, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_BbdFixtureWithoutClosingDate_HasNullClosingDate()
    {
        var url = Detail("computer-science-it-bursaries-south-africa/bbd-bursary/");

        var result = _adapter.Parse(url, Fixture("detail-bbd-bursary.html"));

        Assert.True(result.IsSuccess);
        var listing = result.Value;
        Assert.Equal("BBD Bursary South Africa 2025 – 2026", listing.Title);
        Assert.Equal("BBD", listing.FunderName);
        Assert.Null(listing.ClosingDate);
        Assert.Equal(new Uri("https://bbdsoftware.com/jobs/bursary-beyond-programme/"), listing.ApplyUrl);
        Assert.Contains("passionate about technology", listing.EligibilityText, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_IndexFixture_ReturnsNotABursary()
    {
        var result = _adapter.Parse(BaseUrl, Fixture("index-1.html"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Parse.NotABursary", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Parse_PageWithTitleButNoEligibility_ReturnsNotABursary()
    {
        var result = _adapter.Parse(BaseUrl, "<div class='entry-content'><h1>Some news</h1><h3>ABOUT</h3><p>Hello</p></div>");

        Assert.Equal("Parse.NotABursary", Assert.Single(result.Errors).Code);
    }
}
