using GetABursary.Worker.Sources;
using GetABursary.Worker.Sources.Adapters.AllBursaries;
using GetABursary.Worker.Tests.Pipeline;

namespace GetABursary.Worker.Tests.Sources.Adapters.AllBursaries;

public class AllBursariesAdapterTests
{
    private static readonly Uri BaseUrl = new("https://allbursaries.co.za");
    private static readonly AllBursariesAdapter Adapter = new();

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "allbursaries", name));

    private static Uri DetailUrl(string slug) => new($"https://allbursaries.co.za/accounting/{slug}");

    private static FakeFetcher IndexWithAccounting()
    {
        var fetcher = new FakeFetcher();
        fetcher.Pages[new Uri(BaseUrl, "bursaries")] = Fixture("index-1.html");
        fetcher.Pages[new Uri(BaseUrl, "accounting")] = Fixture("category-accounting.html");
        return fetcher;
    }

    private static async Task<List<Uri>> Discover(FakeFetcher fetcher, SourceOptions options)
    {
        var urls = new List<Uri>();
        await foreach (var url in Adapter.DiscoverAsync(fetcher, options, CancellationToken.None))
        {
            urls.Add(url);
        }

        return urls;
    }

    [Fact]
    public async Task Discover_FromIndexFixture_ReturnsDetailUrls()
    {
        // Only the accounting category page is served; the other categories fail to fetch and are skipped.
        var urls = await Discover(IndexWithAccounting(), new SourceOptions(BaseUrl, 0));

        Assert.Equal(15, urls.Count);
        Assert.Equal(urls.Count, urls.Distinct().Count());
        Assert.Contains(DetailUrl("agsa-bursary"), urls);
        Assert.DoesNotContain(urls, u => u.AbsolutePath.StartsWith("/general", StringComparison.Ordinal)); // other section
        Assert.DoesNotContain(urls, u => u.Segments.Length != 3); // no category or nav pages
    }

    [Fact]
    public async Task Discover_MaxListings_StopsAtTheCap()
    {
        var urls = await Discover(IndexWithAccounting(), new SourceOptions(BaseUrl, 0, MaxListings: 5));

        Assert.Equal(5, urls.Count);
    }

    [Fact]
    public async Task Discover_MaxPagesOne_FetchesOnlyTheIndex()
    {
        var urls = await Discover(IndexWithAccounting(), new SourceOptions(BaseUrl, 0, MaxPages: 1));

        Assert.Empty(urls);
    }

    [Fact]
    public async Task Discover_IndexFetchFails_YieldsNothing()
    {
        var urls = await Discover(new FakeFetcher(), new SourceOptions(BaseUrl, 0));

        Assert.Empty(urls);
    }

    [Fact]
    public void Parse_AgsaFixture_ReadsDateFunderAndExternalApplyLink()
    {
        var result = Adapter.Parse(DetailUrl("agsa-bursary"), Fixture("detail-agsa-bursary.html"));

        var listing = result.Value;
        Assert.Equal("AGSA Bursary Programme", listing.Title);
        Assert.Equal("Auditor-General of South Africa (AGSA)", listing.FunderName);
        Assert.Equal(new DateOnly(2026, 10, 15), listing.ClosingDate);
        Assert.Contains("minimum 60% in English", listing.EligibilityText);
        Assert.Contains("BCompt", listing.FieldsHint);
        Assert.Contains("Prescribed books", listing.ValueText);
        Assert.Equal("https://apply.agsa.co.za/bursary/application", listing.ApplyUrl?.AbsoluteUri); // not the footer's x.com link
        Assert.True(listing.EligibilityText.Length <= SourceAdapterBase.MaxTextLength);
    }

    [Fact]
    public void Parse_SaipaFixture_HasNoClosingDate()
    {
        var result = Adapter.Parse(DetailUrl("saipa-scholarship"), Fixture("detail-saipa-scholarship.html"));

        var listing = result.Value;
        Assert.Equal("SAIPA Scholarship Programme", listing.Title);
        Assert.Null(listing.ClosingDate); // "To be announced"
        Assert.Contains("Minimum 70% academic average", listing.EligibilityText);
        Assert.Contains("Monthly student allowance", listing.ValueText);
    }

    [Fact]
    public void Parse_MtnFixture_HasNoClosingDateAndKeepsEligibilityOnly()
    {
        var result = Adapter.Parse(DetailUrl("mtn-ca-bursary"), Fixture("detail-mtn-ca-bursary.html"));

        var listing = result.Value;
        Assert.Equal("MTN Chartered Accountant (CA) Bursary Programme", listing.Title);
        Assert.Equal("MTN Group", listing.FunderName);
        Assert.Null(listing.ClosingDate);
        Assert.Contains("Completed BComAcc (Hons) or CTA", listing.EligibilityText);
        Assert.DoesNotContain("Market-related salary throughout training", listing.EligibilityText);
        Assert.Equal("https://www.mtn.com/careers/", listing.ApplyUrl?.AbsoluteUri);
    }

    [Theory]
    [InlineData("index-1.html")]
    [InlineData("category-accounting.html")]
    public void Parse_NonBursaryPage_ReturnsNotABursary(string fixture)
    {
        var result = Adapter.Parse(new Uri(BaseUrl, "bursaries"), Fixture(fixture));

        Assert.True(result.IsFailure);
        Assert.Equal("Parse.NotABursary", Assert.Single(result.Errors).Code);
    }
}
