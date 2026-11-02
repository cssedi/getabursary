using GetABursary.Worker.Sources;
using GetABursary.Worker.Sources.Adapters.StudyTrust;
using GetABursary.Worker.Tests.Pipeline;

namespace GetABursary.Worker.Tests.Sources.Adapters.StudyTrust;

public class StudyTrustAdapterTests
{
    private static readonly Uri BaseUrl = new("https://studytrust.org.za");
    private static readonly StudyTrustAdapter Adapter = new();

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "studytrust", name));

    private static Uri DetailUrl(string slug) => new($"https://studytrust.org.za/{slug}/");

    [Fact]
    public async Task Discover_FromIndexFixture_ReturnsDetailUrls()
    {
        var fetcher = new FakeFetcher();
        fetcher.Pages[new Uri(BaseUrl, "bursaries/")] = Fixture("index-1.html");

        var urls = new List<Uri>();
        await foreach (var url in Adapter.DiscoverAsync(fetcher, new SourceOptions(BaseUrl, 0), CancellationToken.None))
        {
            urls.Add(url);
        }

        Assert.Equal(33, urls.Count);
        Assert.Equal(urls.Count, urls.Distinct().Count());
        Assert.All(urls, u => Assert.Equal("studytrust.org.za", u.Host));
        Assert.Contains(DetailUrl("amazon-recruitment-bursaries"), urls); // relative link on the page
        Assert.Contains(DetailUrl("hp-inc-bursaries"), urls); // absolute link on the page
        Assert.DoesNotContain(urls, u => u.AbsolutePath is "/" or "/bursaries/");
    }

    [Fact]
    public async Task Discover_MaxListings_StopsAtTheCap()
    {
        var fetcher = new FakeFetcher();
        fetcher.Pages[new Uri(BaseUrl, "bursaries/")] = Fixture("index-1.html");

        var urls = new List<Uri>();
        await foreach (var url in Adapter.DiscoverAsync(fetcher, new SourceOptions(BaseUrl, 0, MaxListings: 5), CancellationToken.None))
        {
            urls.Add(url);
        }

        Assert.Equal(5, urls.Count);
    }

    [Fact]
    public async Task Discover_IndexFetchFails_YieldsNothing()
    {
        var urls = new List<Uri>();
        await foreach (var url in Adapter.DiscoverAsync(new FakeFetcher(), new SourceOptions(BaseUrl, 0), CancellationToken.None))
        {
            urls.Add(url);
        }

        Assert.Empty(urls);
    }

    [Fact]
    public void Parse_AmazonRecruitmentFixture_ReadsDatesAndExternalApplyLink()
    {
        var result = Adapter.Parse(DetailUrl("amazon-recruitment-bursaries"), Fixture("detail-amazon-recruitment-bursaries.html"));

        var listing = result.Value;
        Assert.Equal("Amazon Recruitment Bursary (Software Development Engineer)", listing.Title);
        Assert.Equal(new DateOnly(2026, 11, 11), listing.ClosingDate);
        Assert.Null(listing.OpenDate); // "01 June" has no year
        Assert.Contains("An average of 65% and above", listing.EligibilityText);
        Assert.Contains("A personal computer", listing.ValueText);
        Assert.StartsWith("https://studytrust.kycdd.co.za/", listing.ApplyUrl?.AbsoluteUri);
        Assert.True(listing.EligibilityText.Length <= SourceAdapterBase.MaxTextLength);
    }

    [Fact]
    public void Parse_DecTrustFixture_HasFieldsHintAndNoClosingDate()
    {
        var result = Adapter.Parse(DetailUrl("dec-trust"), Fixture("detail-dec-trust.html"));

        var listing = result.Value;
        Assert.Equal("DEC Trust Bursary for Students with Disabilities", listing.Title);
        Assert.Null(listing.ClosingDate);
        Assert.Contains("Must be a South African citizen", listing.EligibilityText);
        Assert.Contains("Actuarial Science", listing.FieldsHint);
        Assert.DoesNotContain("Actuarial Science", listing.EligibilityText);
        Assert.Contains("Laptop", listing.ValueText);
    }

    [Fact]
    public void Parse_InvestecFixture_HasNoClosingDateOrApplyLink()
    {
        var result = Adapter.Parse(DetailUrl("investec"), Fixture("detail-investec.html"));

        var listing = result.Value;
        Assert.Equal("Investec CSI Bursary", listing.Title);
        Assert.Null(listing.ClosingDate);
        Assert.Null(listing.ApplyUrl);
        Assert.Contains("Only applications from South African citizens will be considered", listing.EligibilityText);
    }

    [Fact]
    public void Parse_IndexFixture_ReturnsNotABursary()
    {
        var result = Adapter.Parse(new Uri(BaseUrl, "bursaries/"), Fixture("index-1.html"));

        Assert.True(result.IsFailure);
        Assert.Equal("Parse.NotABursary", Assert.Single(result.Errors).Code);
    }
}
