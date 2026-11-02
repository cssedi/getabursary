using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using GetABursary.Api.Tests.Auth;
using GetABursary.Core.Common;
using GetABursary.Core.Data;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;
using GetABursary.Core.Domain.Reports;
using GetABursary.Core.Repositories.Bursaries;
using GetABursary.Core.Repositories.Reports;
using GetABursary.Worker.Extraction;
using GetABursary.Worker.Pipeline.Merging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SourceListing = GetABursary.Worker.Sources.RawListing;

namespace GetABursary.Api.Tests.Admin;

[Collection(AuthFixtureDefinition.Name)]
public sealed partial class AdminEndpointTests(AuthFixture fixture)
{
    private const string Password = "Passw0rd!";

    [Fact]
    public async Task AdminEndpoints_SignedOut_Return401()
    {
        using var client = fixture.Factory.CreateClient();

        foreach (var (method, url) in Endpoints())
        {
            using var response = await client.SendAsync(new HttpRequestMessage(method, url) { Content = JsonContent.Create(new { }) });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task AdminEndpoints_Student_Return403()
    {
        using var client = await SignInAsync($"student-{Guid.NewGuid():N}@example.test");

        foreach (var (method, url) in Endpoints())
        {
            using var response = await client.SendAsync(new HttpRequestMessage(method, url) { Content = JsonContent.Create(new { }) });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task ListReviewItems_FilterByType_ReturnsOpenItemsWithBursaryAndReport()
    {
        var bursary = await AddBursaryAsync();
        var newListing = await AddReviewItemAsync(bursary, ReviewItemType.NewListing);
        var report = await AddReviewItemAsync(bursary, ReviewItemType.UserReport, withReport: true);
        using var client = await SignInAdminAsync();

        var all = await GetJsonAsync(client, "/api/v1/admin/review-items");
        var reports = await GetJsonAsync(client, "/api/v1/admin/review-items?type=UserReport");

        Assert.Contains(all.GetProperty("items").EnumerateArray(), i => i.GetProperty("id").GetGuid() == newListing);
        var items = reports.GetProperty("items").EnumerateArray().ToList();
        Assert.DoesNotContain(items, i => i.GetProperty("id").GetGuid() == newListing);
        var item = Assert.Single(items, i => i.GetProperty("id").GetGuid() == report);
        Assert.Equal(bursary.Name, item.GetProperty("bursaryName").GetString());
        Assert.Equal("Dates look wrong", item.GetProperty("reportComment").GetString());
        Assert.Equal("WrongClosingDate", item.GetProperty("reportReason").GetString());
    }

    [Fact]
    public async Task Resolve_Publish_BursaryAppearsInPublicSearch()
    {
        var bursary = await AddBursaryAsync();
        var itemId = await AddReviewItemAsync(bursary, ReviewItemType.NewListing);
        using var client = await SignInAdminAsync();
        Assert.DoesNotContain(await PublicSearchIdsAsync(client), id => id == bursary.Id);

        using var response = await ResolveAsync(client, itemId, "Publish", "looks good");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(await PublicSearchIdsAsync(client), id => id == bursary.Id);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>();
        var item = await db.ReviewItems.AsNoTracking().SingleAsync(r => r.Id == itemId);
        Assert.Equal(ReviewStatus.Resolved, item.Status);
        Assert.Equal("looks good", item.Notes);
        Assert.NotNull(item.ResolvedByUserId);
        Assert.NotNull(item.ResolvedAt);
    }

    [Fact]
    public async Task Resolve_Dismiss_LeavesBursaryUnpublishedAndItemDismissed()
    {
        var bursary = await AddBursaryAsync();
        var itemId = await AddReviewItemAsync(bursary, ReviewItemType.LowConfidence);
        using var client = await SignInAdminAsync();

        using var response = await ResolveAsync(client, itemId, "Dismiss", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await GetJsonAsync(client, $"/api/v1/admin/bursaries/{bursary.Id}");
        Assert.False(detail.GetProperty("isPublished").GetBoolean());
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>();
        Assert.Equal(ReviewStatus.Dismissed, (await db.ReviewItems.AsNoTracking().SingleAsync(r => r.Id == itemId)).Status);
    }

    [Fact]
    public async Task Resolve_MarkClosed_ClosesAndLocksStatusAndRejectsSecondResolve()
    {
        var bursary = await AddBursaryAsync(published: true);
        var itemId = await AddReviewItemAsync(bursary, ReviewItemType.UserReport, withReport: true);
        using var client = await SignInAdminAsync();

        using var response = await ResolveAsync(client, itemId, "MarkClosed", null);
        using var again = await ResolveAsync(client, itemId, "Publish", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        var detail = await GetJsonAsync(client, $"/api/v1/admin/bursaries/{bursary.Id}");
        Assert.Equal("Closed", detail.GetProperty("status").GetString());
        Assert.Contains("Status", detail.GetProperty("lockedFields").EnumerateArray().Select(f => f.GetString()));
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>();
        Assert.Equal(ReviewStatus.Resolved, (await db.ListingReports.AsNoTracking().SingleAsync(r => r.BursaryId == bursary.Id)).Status);
    }

    [Fact]
    public async Task SearchBursaries_ByNameAndPublished_IncludesUnpublished()
    {
        var bursary = await AddBursaryAsync(name: $"Zxqvortex Mining Award {Guid.NewGuid():N}");
        using var client = await SignInAdminAsync();

        var found = await GetJsonAsync(client, "/api/v1/admin/bursaries?query=zxqvortex");
        var publishedOnly = await GetJsonAsync(client, "/api/v1/admin/bursaries?query=zxqvortex&published=true");

        Assert.Contains(found.GetProperty("items").EnumerateArray(), i => i.GetProperty("id").GetGuid() == bursary.Id);
        Assert.Empty(publishedOnly.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Patch_ChangesSubset_LocksOnlyChangedFields()
    {
        var bursary = await AddBursaryAsync(name: "Original Name");
        using var client = await SignInAdminAsync();

        using var response = await PatchAsync(client, bursary.Id, new
        {
            name = "Original Name",
            summary = "A better summary",
            minAverage = 65.5,
            closingDate = "2099-03-31",
            studyLevels = (string[])["Honours"],
            funderName = "Patch Funder Trust",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("A better summary", body.GetProperty("summary").GetString());
        Assert.Equal("Patch Funder Trust", body.GetProperty("funderName").GetString());
        var locked = body.GetProperty("lockedFields").EnumerateArray().Select(f => f.GetString()).Order().ToList();
        Assert.Equal(["ClosingDate", "FunderId", "MinAverage", "StudyLevels", "Summary"], locked);
    }

    [Fact]
    public async Task Patch_IsPublished_PublishesAndUnpublishesWithoutLocking()
    {
        var bursary = await AddBursaryAsync();
        using var client = await SignInAdminAsync();

        using var publish = await PatchAsync(client, bursary.Id, new { isPublished = true });
        var published = await publish.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(published.GetProperty("isPublished").GetBoolean());
        Assert.Contains(bursary.Id, await PublicSearchIdsAsync(client));

        using var unpublish = await PatchAsync(client, bursary.Id, new { isPublished = false });
        var unpublished = await unpublish.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(unpublished.GetProperty("isPublished").GetBoolean());
        Assert.Empty(unpublished.GetProperty("lockedFields").EnumerateArray());
        Assert.DoesNotContain(bursary.Id, await PublicSearchIdsAsync(client));

        using var bad = await PatchAsync(client, bursary.Id, new { isPublished = "yes" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Patch_InvalidValues_Return400AndChangeNothing()
    {
        var bursary = await AddBursaryAsync();
        using var client = await SignInAdminAsync();

        using var bad = await PatchAsync(client, bursary.Id, new { name = "Changed", minAverage = 150, bogus = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var detail = await GetJsonAsync(client, $"/api/v1/admin/bursaries/{bursary.Id}");
        Assert.Equal(bursary.Name, detail.GetProperty("name").GetString());
        Assert.Empty(detail.GetProperty("lockedFields").EnumerateArray());
    }

    [Fact]
    public async Task Patch_NullFieldCode_Returns400()
    {
        var bursary = await AddBursaryAsync();
        using var client = await SignInAdminAsync();

        using var bad = await client.PatchAsync(
            $"/api/v1/admin/bursaries/{bursary.Id}",
            new StringContent("{\"fieldCodes\":[null]}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Merge_AfterPatch_DoesNotOverwriteLockedFieldButUpdatesOthers()
    {
        var bursary = await AddBursaryAsync(name: "Merge Lock Original");
        var sourceId = await AddRawListingAsync(bursary, "https://merge.example.test/lock");
        using var client = await SignInAdminAsync();
        using var patch = await PatchAsync(client, bursary.Id, new { name = "Admin Edited Name" });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>();
        var merger = new BursaryMerger(new BursaryWriteRepository(db), new FunderRepository(db), new ReviewItemRepository(db), new SystemClock());
        var raw = new SourceListing(new Uri("https://merge.example.test/lock"), "Ingested Title", null, null, null, null, "text", null, null);
        var extracted = new ExtractedEligibility(
            [], false, [], [], [StudyLevel.Undergraduate], null, [], Citizenship.SouthAfrican, [], [CoverageItem.Tuition],
            null, null, null, null, null, "Ingested summary", 0.9m);
        var rawId = (await db.RawListings.AsNoTracking().SingleAsync(r => r.BursaryId == bursary.Id)).Id;
        await merger.MergeAsync(sourceId, raw.SourceUrl, rawId, raw, extracted, CancellationToken.None);

        var detail = await GetJsonAsync(client, $"/api/v1/admin/bursaries/{bursary.Id}");
        Assert.Equal("Admin Edited Name", detail.GetProperty("name").GetString());
        Assert.Equal("Ingested summary", detail.GetProperty("summary").GetString());
    }

    [Fact]
    public async Task Unlock_LockedField_RemovesOnlyThatLock()
    {
        var bursary = await AddBursaryAsync();
        using var client = await SignInAdminAsync();
        using var patch = await PatchAsync(client, bursary.Id, new { name = "Unlock Me", summary = "Keep locked" });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        using var response = await client.DeleteAsync($"/api/v1/admin/bursaries/{bursary.Id}/locks/Name");
        using var missing = await client.DeleteAsync($"/api/v1/admin/bursaries/{bursary.Id}/locks/Name");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(["Summary"], body.GetProperty("lockedFields").EnumerateArray().Select(f => f.GetString()));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task ListSources_WithAndWithoutRun_ShowsLatestRunCounts()
    {
        var sourceId = await AddSourceAsync();
        using var client = await SignInAdminAsync();
        Assert.Equal(JsonValueKind.Null, FindSource(await GetSourcesAsync(client), sourceId).GetProperty("lastRun").ValueKind);

        await AddRunAsync(sourceId, DateTimeOffset.UtcNow.AddDays(-2), discovered: 1);
        await AddRunAsync(sourceId, DateTimeOffset.UtcNow.AddDays(-1), discovered: 5, errorSummary: "Timeout on page 3");

        var lastRun = FindSource(await GetSourcesAsync(client), sourceId).GetProperty("lastRun");
        Assert.Equal(5, lastRun.GetProperty("discovered").GetInt32());
        Assert.Equal("Timeout on page 3", lastRun.GetProperty("errorSummary").GetString());
        Assert.Equal("Succeeded", lastRun.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ListSources_NoRunYet_LastRunIsNull()
    {
        var sourceId = await AddSourceAsync();
        using var client = await SignInAdminAsync();

        var source = FindSource(await GetSourcesAsync(client), sourceId);

        Assert.Equal(JsonValueKind.Null, source.GetProperty("lastRun").ValueKind);
    }

    [Fact]
    public async Task PatchSource_Toggles_Rejects_MissingEnabled_And_UnknownId()
    {
        var sourceId = await AddSourceAsync(enabled: true);
        using var client = await SignInAdminAsync();

        using var off = await client.PatchAsJsonAsync($"/api/v1/admin/sources/{sourceId}", new { enabled = false });
        using var missing = await client.PatchAsJsonAsync($"/api/v1/admin/sources/{sourceId}", new { });
        using var unknown = await client.PatchAsJsonAsync($"/api/v1/admin/sources/{Guid.NewGuid()}", new { enabled = true });

        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.False((await off.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("enabled").GetBoolean());
        Assert.False(FindSource(await GetSourcesAsync(client), sourceId).GetProperty("enabled").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task RunNow_TwiceForSameSource_QueuesOneRequest()
    {
        var sourceId = await AddSourceAsync();
        using var client = await SignInAdminAsync();

        using var first = await client.PostAsJsonAsync("/api/v1/admin/ingestion-runs", new { sourceId });
        using var second = await client.PostAsJsonAsync("/api/v1/admin/ingestion-runs", new { sourceId });
        using var all = await client.PostAsJsonAsync("/api/v1/admin/ingestion-runs", new { });
        using var unknown = await client.PostAsJsonAsync("/api/v1/admin/ingestion-runs", new { sourceId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, all.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(firstId, (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
        Assert.NotEqual(firstId, (await all.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>();
        Assert.Equal(1, await db.IngestionRunRequests.CountAsync(r => r.SourceId == sourceId && r.PickedUpAt == null));
    }

    [Fact]
    public async Task ListRuns_Pages_ReturnsEveryRunOnceNewestFirst()
    {
        var sourceId = await AddSourceAsync();
        var start = DateTimeOffset.UtcNow.AddDays(-30);
        var runIds = new List<Guid>();
        for (var i = 0; i < 21; i++)
        {
            runIds.Add(await AddRunAsync(sourceId, start.AddHours(i), discovered: i));
        }

        using var client = await SignInAdminAsync();
        var first = await GetJsonAsync(client, "/api/v1/admin/ingestion-runs?page=1");
        var total = first.GetProperty("totalCount").GetInt32();
        var seen = new List<JsonElement>();
        for (var page = 1; page <= (total + 19) / 20; page++)
        {
            var json = await GetJsonAsync(client, $"/api/v1/admin/ingestion-runs?page={page}");
            seen.AddRange(json.GetProperty("items").EnumerateArray().Select(i => i.Clone()));
        }

        Assert.Equal(20, first.GetProperty("items").GetArrayLength());
        var ids = seen.Select(i => i.GetProperty("id").GetGuid()).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(runIds, id => Assert.Contains(id, ids));
        var ours = seen.Single(i => i.GetProperty("id").GetGuid() == runIds[^1]);
        var source = Assert.Single(ours.GetProperty("sources").EnumerateArray());
        Assert.Equal(sourceId, source.GetProperty("sourceId").GetGuid());
        Assert.Equal(20, source.GetProperty("discovered").GetInt32());
        using var badPage = await client.GetAsync("/api/v1/admin/ingestion-runs?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, badPage.StatusCode);
    }

    private static IEnumerable<(HttpMethod Method, string Url)> Endpoints()
    {
        var id = Guid.NewGuid();
        yield return (HttpMethod.Get, "/api/v1/admin/review-items");
        yield return (HttpMethod.Post, $"/api/v1/admin/review-items/{id}/resolve");
        yield return (HttpMethod.Get, "/api/v1/admin/bursaries");
        yield return (HttpMethod.Get, $"/api/v1/admin/bursaries/{id}");
        yield return (HttpMethod.Patch, $"/api/v1/admin/bursaries/{id}");
        yield return (HttpMethod.Delete, $"/api/v1/admin/bursaries/{id}/locks/Name");
        yield return (HttpMethod.Get, "/api/v1/admin/sources");
        yield return (HttpMethod.Patch, $"/api/v1/admin/sources/{id}");
        yield return (HttpMethod.Post, "/api/v1/admin/ingestion-runs");
        yield return (HttpMethod.Get, "/api/v1/admin/ingestion-runs");
    }

    private static async Task<JsonElement> GetSourcesAsync(HttpClient client) =>
        await GetJsonAsync(client, "/api/v1/admin/sources");

    private static JsonElement FindSource(JsonElement sources, Guid id) =>
        sources.EnumerateArray().Single(s => s.GetProperty("id").GetGuid() == id);

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).Clone();
    }

    private static Task<HttpResponseMessage> ResolveAsync(HttpClient client, Guid id, string action, string? notes) =>
        client.PostAsJsonAsync($"/api/v1/admin/review-items/{id}/resolve", new { action, notes });

    private static Task<HttpResponseMessage> PatchAsync(HttpClient client, Guid id, object body) =>
        client.PatchAsJsonAsync($"/api/v1/admin/bursaries/{id}", body);

    private static async Task<List<Guid>> PublicSearchIdsAsync(HttpClient client)
    {
        var json = await GetJsonAsync(client, "/api/v1/bursaries/search?pageSize=50");
        return [.. json.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid())];
    }

    private async Task<Bursary> AddBursaryAsync(string? name = null, bool published = false)
    {
        name ??= $"Admin Test Bursary {Guid.NewGuid():N}";
        var bursary = new Bursary
        {
            Slug = $"admin-{Guid.NewGuid():N}",
            Name = name,
            NormalizedName = NameNormalizer.Normalize(name),
            IsPublished = published,
            Status = BursaryStatus.Open,
            ClosingDate = new DateOnly(2099, 1, 1),
            StudyLevels = [StudyLevel.Undergraduate],
            AllFieldsEligible = true,
        };
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>();
        db.Bursaries.Add(bursary);
        await db.SaveChangesAsync();
        return bursary;
    }

    private async Task<Guid> AddReviewItemAsync(Bursary bursary, ReviewItemType type, bool withReport = false)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>();
        var item = new ReviewItem { Type = type, BursaryId = bursary.Id, Details = JsonDocument.Parse("""{"confidence":0.5}""") };
        if (withReport)
        {
            var report = new ListingReport
            {
                BursaryId = bursary.Id,
                Reason = ReportReason.WrongClosingDate,
                Comment = "Dates look wrong",
                IpHash = "hash",
            };
            db.ListingReports.Add(report);
            item.ListingReportId = report.Id;
        }

        db.ReviewItems.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    private async Task<Guid> AddRawListingAsync(Bursary bursary, string url)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>();
        var source = new Source { Key = $"s{Guid.NewGuid():N}"[..20], Name = "Admin test source", BaseUrl = "https://merge.example.test" };
        db.Sources.Add(source);
        db.RawListings.Add(new RawListing
        {
            SourceId = source.Id,
            Url = url,
            ContentHash = "abc",
            Html = "<html></html>",
            BursaryId = bursary.Id,
        });
        await db.SaveChangesAsync();
        return source.Id;
    }

    private async Task<Guid> AddSourceAsync(bool enabled = true)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>();
        var source = new Source
        {
            Key = $"s{Guid.NewGuid():N}"[..20],
            Name = "Admin source test",
            BaseUrl = "https://sources.example.test",
            Enabled = enabled,
        };
        db.Sources.Add(source);
        await db.SaveChangesAsync();
        return source.Id;
    }

    private async Task<Guid> AddRunAsync(Guid sourceId, DateTimeOffset startedAt, int discovered, string? errorSummary = null)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GetABursaryDbContext>();
        var run = new IngestionRun
        {
            Trigger = IngestionTrigger.Scheduled,
            Status = RunStatus.Succeeded,
            StartedAt = startedAt,
            FinishedAt = startedAt.AddMinutes(5),
        };
        run.Sources.Add(new IngestionRunSource
        {
            SourceId = sourceId,
            Discovered = discovered,
            Created = 1,
            Updated = 2,
            Failed = 0,
            ErrorSummary = errorSummary,
        });
        db.IngestionRuns.Add(run);
        await db.SaveChangesAsync();
        return run.Id;
    }

    private async Task<HttpClient> SignInAdminAsync() => await SignInAsync(AuthFixture.AdminTestsEmail);

    /// <summary>Signs in as the user, registering and confirming the account first if it does not exist yet.</summary>
    private async Task<HttpClient> SignInAsync(string email)
    {
        var client = fixture.Factory.CreateClient();
        using var first = await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email, password = Password });
        if (first.IsSuccessStatusCode)
        {
            return client;
        }

        using var register = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var message = fixture.Emails.Last(e => e.Contains($"Email to {email} ", StringComparison.Ordinal));
        var link = new Uri(UrlPattern().Match(message).Value.Replace("&amp;", "&", StringComparison.Ordinal));
        using var confirm = await client.GetAsync("/api/v1/auth/confirmEmail" + link.Query);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    [GeneratedRegex(@"https://\S+")]
    private static partial Regex UrlPattern();
}
