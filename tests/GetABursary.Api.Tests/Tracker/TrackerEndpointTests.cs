using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GetABursary.Api.Tests.Tracker;

[Collection(TrackerFixtureDefinition.Name)]
public sealed class TrackerEndpointTests(TrackerFixture fixture)
{
    [Fact]
    public async Task Upsert_NewEntry_Returns201WithSavedEntry()
    {
        using var client = await fixture.SignInNewUserAsync();

        using var response = await PutAsync(client, fixture.SoonBursaryId, new { status = "Saved" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Saved", body.GetProperty("status").GetString());
        Assert.Equal(fixture.SoonBursaryId, body.GetProperty("bursary").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Upsert_ExistingEntry_Returns200AndUpdatesFields()
    {
        using var client = await fixture.SignInNewUserAsync();
        using var created = await PutAsync(client, fixture.LaterBursaryId, new { status = "Saved" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var updated = await PutAsync(client, fixture.LaterBursaryId, new { status = "Applied", appliedOn = "2099-01-02", notes = "Sent with certified copies" });

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var entry = (await ListAsync(client)).Single();
        Assert.Equal("Applied", entry.GetProperty("status").GetString());
        Assert.Equal("2099-01-02", entry.GetProperty("appliedOn").GetString());
        Assert.Equal("Sent with certified copies", entry.GetProperty("notes").GetString());
    }

    [Fact]
    public async Task List_SavedBursaries_ClosingSoonestFirstWithUndatedLast()
    {
        using var client = await fixture.SignInNewUserAsync();
        await PutAsync(client, fixture.UndatedBursaryId, new { status = "Saved" });
        await PutAsync(client, fixture.LaterBursaryId, new { status = "Saved" });
        await PutAsync(client, fixture.SoonBursaryId, new { status = "Saved" });

        var ids = (await ListAsync(client))
            .Select(e => e.GetProperty("bursary").GetProperty("id").GetGuid())
            .ToList();

        Assert.Equal([fixture.SoonBursaryId, fixture.LaterBursaryId, fixture.UndatedBursaryId], ids);
    }

    [Fact]
    public async Task Upsert_ConcurrentFirstSaves_NeverReturn500()
    {
        using var client = await fixture.SignInNewUserAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => PutAsync(client, fixture.SoonBursaryId, new { status = "Saved" })));

        Assert.All(responses, r => Assert.True(r.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK));
        Assert.Single(await ListAsync(client));
    }

    [Fact]
    public async Task Delete_SavedEntry_Returns204AndIsIdempotent()
    {
        using var client = await fixture.SignInNewUserAsync();
        await PutAsync(client, fixture.SoonBursaryId, new { status = "Saved" });

        using var first = await client.DeleteAsync($"/api/v1/me/tracker/{fixture.SoonBursaryId}");
        using var second = await client.DeleteAsync($"/api/v1/me/tracker/{fixture.SoonBursaryId}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Empty(await ListAsync(client));
    }

    [Fact]
    public async Task Tracker_SignedOut_Returns401()
    {
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "getabursary");

        using var list = await client.GetAsync("/api/v1/me/tracker");
        using var upsert = await PutAsync(client, fixture.SoonBursaryId, new { status = "Saved" });

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, upsert.StatusCode);
    }

    [Fact]
    public async Task Tracker_OtherUser_CannotSeeOrDeleteEntries()
    {
        using var owner = await fixture.SignInNewUserAsync();
        using var other = await fixture.SignInNewUserAsync();
        await PutAsync(owner, fixture.SoonBursaryId, new { status = "Saved" });

        Assert.Empty(await ListAsync(other));

        using var deleted = await other.DeleteAsync($"/api/v1/me/tracker/{fixture.SoonBursaryId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        var stillSaved = (await ListAsync(owner)).Single();
        Assert.Equal("Saved", stillSaved.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Upsert_UnknownBursary_Returns404()
    {
        using var client = await fixture.SignInNewUserAsync();

        using var response = await PutAsync(client, Guid.CreateVersion7(), new { status = "Saved" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upsert_UnpublishedBursary_Returns404()
    {
        using var client = await fixture.SignInNewUserAsync();

        using var response = await PutAsync(client, fixture.UnpublishedBursaryId, new { status = "Saved" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upsert_NotesLongerThan2000_Returns400()
    {
        using var client = await fixture.SignInNewUserAsync();

        using var response = await PutAsync(client, fixture.SoonBursaryId, new { status = "Saved", notes = new string('x', 2001) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, Guid bursaryId, object body) =>
        client.PutAsync($"/api/v1/me/tracker/{bursaryId}", JsonContent.Create(body));

    private static async Task<List<JsonElement>> ListAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/me/tracker");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
    }
}
