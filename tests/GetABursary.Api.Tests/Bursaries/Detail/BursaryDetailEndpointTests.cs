using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static GetABursary.Api.Tests.Bursaries.Detail.BursaryDetailFixture;

namespace GetABursary.Api.Tests.Bursaries.Detail;

[Collection(BursaryDetailFixtureDefinition.Name)]
public class BursaryDetailEndpointTests(BursaryDetailFixture fixture)
{
    private const string Base = "/api/v1/bursaries";

    [Fact]
    public async Task GetDetail_OpenBursary_Returns200WithFieldsQualificationsAndSources()
    {
        using var response = await fixture.Client.GetAsync($"{Base}/{OpenSlug}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var body = doc!.RootElement;

        Assert.Equal(OpenSlug, body.GetProperty("slug").GetString());
        Assert.Equal("Open", body.GetProperty("status").GetString());
        Assert.Equal("Acme Foundation", body.GetProperty("funderName").GetString());
        Assert.Equal("Summary text", body.GetProperty("summary").GetString());
        Assert.Equal("R50 000 a year", body.GetProperty("valueSummary").GetString());
        Assert.True(body.GetProperty("workBackRequired").GetBoolean());
        Assert.Equal("SouthAfrican", body.GetProperty("citizenship").GetString());
        Assert.Equal("WesternCape", body.GetProperty("provinces")[0].GetString());
        Assert.Equal("Mathematics", body.GetProperty("minSubjectRequirements")[0].GetProperty("subject").GetString());

        var field = Assert.Single(body.GetProperty("fields").EnumerateArray());
        Assert.Equal("0801", field.GetProperty("code").GetString());
        Assert.Equal("Chemical Engineering", field.GetProperty("name").GetString());

        var qualification = Assert.Single(body.GetProperty("qualifications").EnumerateArray());
        Assert.Equal("BSc (Eng) Chemical Engineering", qualification.GetProperty("name").GetString());
        Assert.Equal("UCT", qualification.GetProperty("institutionShortName").GetString());

        var institution = Assert.Single(body.GetProperty("institutions").EnumerateArray());
        Assert.Equal("UCT", institution.GetProperty("shortName").GetString());

        var sources = body.GetProperty("sources").EnumerateArray().ToList();
        Assert.Equal(2, sources.Count);
        Assert.Contains(sources, s => s.GetProperty("key").GetString() == "zabursaries");

        // No funder apply link is stored, so the most recently seen listing is the apply link.
        Assert.Equal("https://trust.example.test/open", body.GetProperty("applyUrl").GetString());
    }

    [Fact]
    public async Task GetDetail_ClosedBursary_Returns410WithSummaryAndNoEligibilityDetails()
    {
        using var response = await fixture.Client.GetAsync($"{Base}/{ClosedSlug}");

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = doc!.RootElement;

        var code = Assert.Single(root.GetProperty("errors").EnumerateArray()).GetProperty("code").GetString();
        Assert.Equal("Bursary.Closed", code);

        var summary = root.GetProperty("bursary");
        Assert.Equal(ClosedSlug, summary.GetProperty("slug").GetString());
        Assert.Equal("Closed", summary.GetProperty("status").GetString());
        Assert.Equal("ZA Bursaries", summary.GetProperty("sources")[0].GetProperty("name").GetString());

        foreach (var eligibilityField in new[] { "summary", "valueSummary", "minSubjectRequirements", "citizenship", "provinces", "fields", "qualifications", "institutions", "workBackDetails", "applyUrl" })
        {
            Assert.False(summary.TryGetProperty(eligibilityField, out _), $"Closed summary leaked '{eligibilityField}'.");
        }
    }

    [Fact]
    public async Task GetDetail_UnpublishedBursary_Returns404()
    {
        using var response = await fixture.Client.GetAsync($"{Base}/{UnpublishedSlug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertErrorCodeAsync(response, "Bursary.NotFound");
    }

    [Fact]
    public async Task GetDetail_UnknownSlug_Returns404()
    {
        using var response = await fixture.Client.GetAsync($"{Base}/no-such-bursary");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertErrorCodeAsync(response, "Bursary.NotFound");
    }

    private static async Task AssertErrorCodeAsync(HttpResponseMessage response, string code)
    {
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var codes = body!.RootElement.GetProperty("errors").EnumerateArray().Select(e => e.GetProperty("code").GetString());
        Assert.Contains(code, codes);
    }
}
