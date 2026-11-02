using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static GetABursary.Api.Tests.Bursaries.Search.BursarySearchFixture;

namespace GetABursary.Api.Tests.Bursaries.Search;

[Collection(BursarySearchFixtureDefinition.Name)]
public class BursarySearchEndpointTests(BursarySearchFixture fixture)
{
    private const string Base = "/api/v1/bursaries/search";

    private static string UctQuery(string extra = "") =>
        $"{Base}?qualificationId={Ids.UctChemEng}&studyLevel=Undergraduate&average=70{extra}";

    [Fact]
    public async Task Search_ByQualification_RanksQualificationAboveFieldAboveAllFields()
    {
        var body = await GetOkAsync(UctQuery());

        Assert.Equal(
            ["Qualification Match", "Second Order Field", "Unknown Level", "First Order Field", "All Fields"],
            Names(body));
        Assert.Equal([3m, 2m, 1.5m, 1m, 0.5m], Items(body).Select(i => i.GetProperty("matchScore").GetDecimal()));
        Assert.Equal(5, body.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Search_UnknownStudyLevel_IsIncludedButRanksLowerThanSameFieldMatch()
    {
        var names = Names(await GetOkAsync(UctQuery())).ToList();

        Assert.Contains("Unknown Level", names);
        Assert.True(names.IndexOf("Second Order Field") < names.IndexOf("Unknown Level"));
        Assert.DoesNotContain("Honours Only", names);
    }

    [Fact]
    public async Task Search_InstitutionRestriction_ExcludesBursariesRestrictedToOtherInstitutions()
    {
        var uct = Names(await GetOkAsync(UctQuery()));
        Assert.DoesNotContain("Wits Only", uct);

        var wits = Names(await GetOkAsync(UctQuery($"&institutionId={Ids.Wits}")));
        Assert.Contains("Wits Only", wits);
    }

    [Fact]
    public async Task Search_QualificationDefaultsInstitutionToItsOwn_WitsQualificationSeesWitsRestricted()
    {
        var names = Names(await GetOkAsync($"{Base}?qualificationId={Ids.WitsChemEng}&studyLevel=Undergraduate&average=70"));

        Assert.Contains("Wits Only", names);
    }

    [Fact]
    public async Task Search_Average_ExcludesBursariesWithHigherMinimum()
    {
        Assert.DoesNotContain("High Minimum", Names(await GetOkAsync(UctQuery())));

        var low = Names(await GetOkAsync($"{Base}?qualificationId={Ids.UctChemEng}&average=50")).ToList();
        Assert.DoesNotContain("Qualification Match", low);
        Assert.DoesNotContain("High Minimum", low);

        var high = Names(await GetOkAsync($"{Base}?qualificationId={Ids.UctChemEng}&average=90"));
        Assert.Contains("High Minimum", high);
    }

    [Fact]
    public async Task Search_ClosedBursaries_HiddenByDefaultAndShownLastWhenIncluded()
    {
        Assert.DoesNotContain("Closed Bursary", Names(await GetOkAsync(UctQuery())));

        var names = Names(await GetOkAsync(UctQuery("&includeClosed=true"))).ToList();
        Assert.Equal("Closed Bursary", names[^1]);
        Assert.Equal(6, names.Count);
    }

    [Fact]
    public async Task Search_ClosingDateRange_FiltersByClosingDate()
    {
        // Fixed today is 2026-06-01: Second closes +10 (06-11), Qualification +30 (07-01), First +60, All +90.
        var names = Names(await GetOkAsync(UctQuery("&closingFrom=2026-06-05&closingTo=2026-07-15")));

        Assert.Equal(["Qualification Match", "Second Order Field"], names);
    }

    [Fact]
    public async Task Search_Paging_ReturnsRequestedPageWithTotalCount()
    {
        var page2 = await GetOkAsync(UctQuery("&page=2&pageSize=2"));

        Assert.Equal(["Unknown Level", "First Order Field"], Names(page2));
        Assert.Equal(5, page2.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, page2.GetProperty("page").GetInt32());
        Assert.Equal(2, page2.GetProperty("pageSize").GetInt32());

        var beyond = await GetOkAsync(UctQuery("&page=9&pageSize=2"));
        Assert.Empty(Items(beyond));
        Assert.Equal(5, beyond.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Search_Sources_AreListedWithIconsAndUrls()
    {
        var body = await GetOkAsync(UctQuery());
        var qual = Items(body).Single(i => i.GetProperty("name").GetString() == "Qualification Match");

        var sources = qual.GetProperty("sources").EnumerateArray().ToList();
        Assert.Equal(2, sources.Count);
        var za = sources.Single(s => s.GetProperty("key").GetString() == "zabursaries");
        Assert.Equal("ZA Bursaries", za.GetProperty("name").GetString());
        Assert.Equal("https://za.example.test/icon.png", za.GetProperty("iconUrl").GetString());
        Assert.Equal("https://za.example.test/qual", za.GetProperty("url").GetString());
        Assert.Equal("Acme Foundation", qual.GetProperty("funderName").GetString());
        Assert.Equal("Open", qual.GetProperty("status").GetString());
        Assert.Equal("Undergraduate", qual.GetProperty("studyLevels")[0].GetString());
        Assert.Equal("Tuition", qual.GetProperty("coverage")[0].GetString());
        Assert.Equal(60m, qual.GetProperty("minAverage").GetDecimal());
    }

    [Fact]
    public async Task Search_ClosingSoon_UsesClockAndFourteenDayWindow()
    {
        var items = Items(await GetOkAsync(UctQuery())).ToList();

        Assert.True(Soon(items, "Second Order Field"));      // closes in 10 days
        Assert.False(Soon(items, "Qualification Match"));    // closes in 30 days
        Assert.False(Soon(items, "Unknown Level"));          // no closing date
    }

    [Fact]
    public async Task Search_ClosingSoonestSort_OrdersByClosingDateNullsLast()
    {
        var names = Names(await GetOkAsync(UctQuery("&sort=ClosingSoonest")));

        Assert.Equal(
            ["Second Order Field", "Qualification Match", "First Order Field", "All Fields", "Unknown Level"],
            names);
    }

    [Fact]
    public async Task Search_NewestSort_ReturnsAllEligibleBursaries()
    {
        var names = Names(await GetOkAsync(UctQuery("&sort=Newest"))).ToList();

        Assert.Equal(5, names.Count);
    }

    [Fact]
    public async Task Search_NoQualificationOrFields_SkipsFieldFilter()
    {
        var names = Names(await GetOkAsync($"{Base}?pageSize=50")).ToList();

        Assert.Contains("Unrelated Field", names);
        Assert.Contains("Honours Only", names);
        Assert.DoesNotContain("Unpublished", names);
        Assert.DoesNotContain("Closed Bursary", names);
    }

    [Fact]
    public async Task Search_ExplicitFieldIds_FilterToThoseFields()
    {
        var names = Names(await GetOkAsync($"{Base}?fieldIds={Ids.LifeSciences}")).ToList();

        Assert.Equal(["Unrelated Field", "All Fields"], names);
    }

    [Fact]
    public async Task Search_RunsASingleSqlQuery()
    {
        fixture.Sql.Clear();

        await GetOkAsync(UctQuery("&includeClosed=true"));

        var commands = fixture.Sql.Commands;
        var command = Assert.Single(commands, c => c.Contains("FROM bursary", StringComparison.Ordinal));
        Assert.DoesNotContain(commands, c => c != command && c.Contains("bursary_source", StringComparison.Ordinal));
        // The qualification lookup is a separate, bursary-independent query; nothing is issued per result.
        Assert.True(commands.Count <= 2, $"Expected at most 2 commands, saw {commands.Count}.");
    }

    [Theory]
    [InlineData("average=-1", "Search.InvalidAverage")]
    [InlineData("average=100.5", "Search.InvalidAverage")]
    [InlineData("pageSize=0", "Search.InvalidPageSize")]
    [InlineData("pageSize=51", "Search.InvalidPageSize")]
    [InlineData("page=0", "Search.InvalidPage")]
    [InlineData("closingFrom=2026-08-01&closingTo=2026-07-01", "Search.InvalidDateRange")]
    public async Task Search_InvalidParameters_Return400WithErrorCode(string query, string code)
    {
        using var response = await fixture.Client.GetAsync($"{Base}?{query}");

        await AssertProblemAsync(response, code);
    }

    [Fact]
    public async Task Search_UnknownQualification_Returns400()
    {
        using var response = await fixture.Client.GetAsync($"{Base}?qualificationId={Guid.NewGuid()}");

        await AssertProblemAsync(response, "Search.UnknownQualification");
    }

    [Fact]
    public async Task Search_BoundaryValues_AreAccepted()
    {
        using var response = await fixture.Client.GetAsync($"{Base}?average=0&pageSize=50&page=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var top = await fixture.Client.GetAsync($"{Base}?average=100&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, top.StatusCode);
    }

    [Fact]
    public async Task Search_MalformedEnum_Returns400()
    {
        using var response = await fixture.Client.GetAsync($"{Base}?studyLevel=Nonsense");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, string code)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var codes = body!.RootElement.GetProperty("errors").EnumerateArray()
            .Select(e => e.GetProperty("code").GetString());
        Assert.Contains(code, codes);
    }

    private static bool Soon(IEnumerable<JsonElement> items, string name) =>
        items.Single(i => i.GetProperty("name").GetString() == name).GetProperty("isClosingSoon").GetBoolean();

    private static JsonElement.ArrayEnumerator Items(JsonElement body) => body.GetProperty("items").EnumerateArray();

    private static IEnumerable<string?> Names(JsonElement body) => Items(body).Select(i => i.GetProperty("name").GetString());

    private async Task<JsonElement> GetOkAsync(string url)
    {
        using var response = await fixture.Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.Clone();
    }
}
