using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GetABursary.Api.Services.Reports;
using GetABursary.Core.Domain.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Api.Tests.Reports;

/// <summary>
/// Integration tests for POST /api/v1/bursaries/{id}/reports against a real PostgreSQL database.
/// These assert on the rows that were actually written, not on in-memory fakes.
/// </summary>
[Collection(ReportsPostgresFixtureDefinition.Name)]
public class ReportsPostgresTests(ReportsPostgresFixture fixture)
{
    private const string Salt = "postgres-test-salt";
    private const string ValidBody = """{"reason":0,"comment":"The closing date is wrong.","contactEmail":"student@example.test"}""";

    [Fact]
    public async Task Post_ValidReport_Returns201AndWritesReportAndLinkedReviewItemRows()
    {
        var bursaryId = await fixture.SeedBursaryAsync();
        using var factory = fixture.CreateApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(ReportsUrl(bursaryId), JsonBody(ValidBody));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var reportId = body!.RootElement.GetProperty("id").GetGuid();

        await using var db = fixture.CreateContext();
        var report = await db.ListingReports.AsNoTracking().SingleAsync(r => r.Id == reportId);
        Assert.Equal(bursaryId, report.BursaryId);
        Assert.Equal(ReportReason.WrongClosingDate, report.Reason);
        Assert.Equal("The closing date is wrong.", report.Comment);
        Assert.Equal("student@example.test", report.ContactEmail);
        Assert.Equal(ReviewStatus.Open, report.Status);
        Assert.Equal(ExpectedIpHash(ClientIp.Unknown), report.IpHash);

        var item = await db.ReviewItems.AsNoTracking().SingleAsync(i => i.ListingReportId == reportId);
        Assert.Equal(ReviewItemType.UserReport, item.Type);
        Assert.Equal(ReviewStatus.Open, item.Status);
        Assert.Equal(bursaryId, item.BursaryId);
    }

    [Fact]
    public async Task Post_InvalidBody_Returns400AndWritesNoRows()
    {
        var bursaryId = await fixture.SeedBursaryAsync();
        using var factory = fixture.CreateApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(ReportsUrl(bursaryId), JsonBody("""{"reason":1,"contactEmail":"not-an-email"}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoRowsForBursaryAsync(bursaryId);
    }

    [Fact]
    public async Task Post_UnknownBursary_Returns404AndWritesNoRows()
    {
        var unknownId = Guid.CreateVersion7();
        using var factory = fixture.CreateApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(ReportsUrl(unknownId), JsonBody(ValidBody));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertNoRowsForBursaryAsync(unknownId);
    }

    [Fact]
    public async Task Post_SixthReportFromSameIp_Returns429AndWritesOnlyFiveReports()
    {
        var bursaryId = await fixture.SeedBursaryAsync();
        using var factory = fixture.CreateApiFactory();
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            using var accepted = await client.PostAsync(ReportsUrl(bursaryId), JsonBody(ValidBody));
            Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        }

        using var limited = await client.PostAsync(ReportsUrl(bursaryId), JsonBody(ValidBody));

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        await using var db = fixture.CreateContext();
        Assert.Equal(5, await db.ListingReports.CountAsync(r => r.BursaryId == bursaryId));
        Assert.Equal(5, await db.ReviewItems.CountAsync(i => i.BursaryId == bursaryId && i.Type == ReviewItemType.UserReport));
    }

    private static string ReportsUrl(Guid bursaryId) => $"/api/v1/bursaries/{bursaryId}/reports";

    private static StringContent JsonBody(string json) => new(json, Encoding.UTF8, "application/json");

    private static string ExpectedIpHash(string ip) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Salt + ip)));

    private async Task AssertNoRowsForBursaryAsync(Guid bursaryId)
    {
        await using var db = fixture.CreateContext();
        Assert.Equal(0, await db.ListingReports.CountAsync(r => r.BursaryId == bursaryId));
        Assert.Equal(0, await db.ReviewItems.CountAsync(i => i.BursaryId == bursaryId));
    }
}
