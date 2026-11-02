using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GetABursary.Core.Repositories.Reports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace GetABursary.Api.Tests.Reports;

/// <summary>
/// Endpoint tests for POST /api/v1/bursaries/{id}/reports. Each test builds its own host so the
/// in-memory rate-limit counters start at zero.
/// </summary>
public class ReportsEndpointTests
{
    private const string ValidBody = """{"reason":0,"comment":"The closing date is wrong.","contactEmail":"student@example.test"}""";

    [Fact]
    public async Task Post_ValidReport_Returns201WithIdAndStoresReportAndReviewItem()
    {
        var bursaryId = Guid.CreateVersion7();
        var repository = new FakeListingReportRepository { KnownBursaryIds = [bursaryId] };
        using var factory = new ReportsApiFactory(repository);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(ReportsUrl(bursaryId), JsonBody(ValidBody));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var id = body!.RootElement.GetProperty("id").GetGuid();

        var report = Assert.Single(repository.Reports);
        Assert.Equal(id, report.Id);
        Assert.Equal(bursaryId, report.BursaryId);
        var item = Assert.Single(repository.ReviewItems);
        Assert.Same(report, item.ListingReport);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task Post_UnknownReason_Returns400WithFieldError()
    {
        var bursaryId = Guid.CreateVersion7();
        var repository = new FakeListingReportRepository { KnownBursaryIds = [bursaryId] };
        using var factory = new ReportsApiFactory(repository);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(ReportsUrl(bursaryId), JsonBody("""{"reason":99}"""));

        await AssertValidationErrorAsync(response, "reason");
        Assert.Empty(repository.Reports);
    }

    [Fact]
    public async Task Post_MissingReason_Returns400WithFieldError()
    {
        var bursaryId = Guid.CreateVersion7();
        var repository = new FakeListingReportRepository { KnownBursaryIds = [bursaryId] };
        using var factory = new ReportsApiFactory(repository);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(ReportsUrl(bursaryId), JsonBody("""{"comment":"no reason given"}"""));

        await AssertValidationErrorAsync(response, "reason");
        Assert.Empty(repository.Reports);
    }

    [Fact]
    public async Task Post_InvalidEmail_Returns400WithFieldError()
    {
        var bursaryId = Guid.CreateVersion7();
        var repository = new FakeListingReportRepository { KnownBursaryIds = [bursaryId] };
        using var factory = new ReportsApiFactory(repository);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(ReportsUrl(bursaryId), JsonBody("""{"reason":1,"contactEmail":"not-an-email"}"""));

        await AssertValidationErrorAsync(response, "contactEmail");
        Assert.Empty(repository.Reports);
    }

    [Fact]
    public async Task Post_CommentOver1000Characters_Returns400WithFieldError()
    {
        var bursaryId = Guid.CreateVersion7();
        var repository = new FakeListingReportRepository { KnownBursaryIds = [bursaryId] };
        using var factory = new ReportsApiFactory(repository);
        using var client = factory.CreateClient();
        var comment = new string('x', 1001);

        using var response = await client.PostAsync(ReportsUrl(bursaryId), JsonBody($$"""{"reason":5,"comment":"{{comment}}"}"""));

        await AssertValidationErrorAsync(response, "comment");
        Assert.Empty(repository.Reports);
    }

    [Fact]
    public async Task Post_UnknownBursary_Returns404AndStoresNothing()
    {
        var repository = new FakeListingReportRepository();
        using var factory = new ReportsApiFactory(repository);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(ReportsUrl(Guid.CreateVersion7()), JsonBody(ValidBody));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(repository.Reports);
        Assert.Empty(repository.ReviewItems);
    }

    [Fact]
    public async Task Post_SixthReportFromSameIp_Returns429WithProblemDetailsAndRetryAfter()
    {
        var bursaryId = Guid.CreateVersion7();
        var repository = new FakeListingReportRepository { KnownBursaryIds = [bursaryId] };
        using var factory = new ReportsApiFactory(repository);
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            using var accepted = await client.PostAsync(ReportsUrl(bursaryId), JsonBody(ValidBody));
            Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        }

        using var limited = await client.PostAsync(ReportsUrl(bursaryId), JsonBody(ValidBody));

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("application/problem+json", limited.Content.Headers.ContentType?.MediaType);
        Assert.True(limited.Headers.Contains("Retry-After"));
        using var problem = await limited.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(429, problem!.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(5, repository.Reports.Count);
    }

    private static string ReportsUrl(Guid bursaryId) => $"/api/v1/bursaries/{bursaryId}/reports";

    private static StringContent JsonBody(string json) => new(json, System.Text.Encoding.UTF8, "application/json");

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var errors = body!.RootElement.GetProperty("errors");
        Assert.True(
            errors.EnumerateObject().Any(p => string.Equals(p.Name, field, StringComparison.OrdinalIgnoreCase)),
            $"Expected a validation error for '{field}'.");
    }

    private sealed class ReportsApiFactory(FakeListingReportRepository repository) : WebApplicationFactory<Program>
    {
        protected override void ConfigureClient(HttpClient client) =>
            client.DefaultRequestHeaders.Add("X-Requested-With", "getabursary");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("Reports:IpHashSalt", "endpoint-test-salt");
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IListingReportRepository>(repository));
        }
    }
}
