using System.Text.Json;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;
using GetABursary.Core.Tests.Data.Taxonomy;
using GetABursary.Core.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Tests.Data.Ingestion;

[Collection("Postgres")]
public class IngestionPersistenceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task RawListing_Saves_And_Reloads_With_Jsonb_Columns()
    {
        var source = new Source { Key = UniqueKey(), Name = "Raw Source", BaseUrl = "https://raw.test" };
        var bursary = new Bursary { Slug = TaxonomyPersistenceTests.UniqueSlug(), Name = "Raw Bursary", NormalizedName = "raw bursary" };
        var fetched = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);
        var listing = new RawListing
        {
            Source = source,
            Url = "https://raw.test/listing/" + Guid.NewGuid().ToString("N"),
            ContentHash = new string('a', 64),
            Html = "<html><body>Listing</body></html>",
            Parsed = JsonDocument.Parse("""{"title":"Acme","closing":"2026-09-30"}"""),
            Extracted = JsonDocument.Parse("""{"confidence":0.91,"fields":["a","b"]}"""),
            Bursary = bursary,
            FirstFetchedAt = fetched,
            LastFetchedAt = fetched.AddDays(1),
            LastChangedAt = fetched,
            MissedRuns = 2,
        };

        await using (var writer = fixture.CreateContext())
        {
            writer.RawListings.Add(listing);
            await writer.SaveChangesAsync();
        }

        await using var reader = fixture.CreateContext();
        var loaded = await reader.RawListings
            .AsNoTracking()
            .SingleAsync(r => r.Id == listing.Id);

        Assert.Equal(source.Id, loaded.SourceId);
        Assert.Equal(listing.Url, loaded.Url);
        Assert.Equal(new string('a', 64), loaded.ContentHash);
        Assert.Equal("<html><body>Listing</body></html>", loaded.Html);
        Assert.NotNull(loaded.Parsed);
        Assert.Equal("Acme", loaded.Parsed!.RootElement.GetProperty("title").GetString());
        Assert.Equal("2026-09-30", loaded.Parsed.RootElement.GetProperty("closing").GetString());
        Assert.NotNull(loaded.Extracted);
        Assert.Equal(0.91m, loaded.Extracted!.RootElement.GetProperty("confidence").GetDecimal());
        Assert.Equal(2, loaded.Extracted.RootElement.GetProperty("fields").GetArrayLength());
        Assert.Equal(bursary.Id, loaded.BursaryId);
        Assert.Equal(fetched, loaded.FirstFetchedAt);
        Assert.Equal(fetched.AddDays(1), loaded.LastFetchedAt);
        Assert.Equal(fetched, loaded.LastChangedAt);
        Assert.Equal(2, loaded.MissedRuns);
    }

    [Fact]
    public async Task RawListing_Null_Jsonb_And_Bursary_Reload_As_Null()
    {
        var source = new Source { Key = UniqueKey(), Name = "Bare Source", BaseUrl = "https://bare.test" };
        var now = DateTimeOffset.UtcNow;
        var listing = new RawListing
        {
            Source = source,
            Url = "https://bare.test/" + Guid.NewGuid().ToString("N"),
            ContentHash = new string('b', 64),
            Html = "<p>x</p>",
            FirstFetchedAt = now,
            LastFetchedAt = now,
            LastChangedAt = now,
        };

        await using (var writer = fixture.CreateContext())
        {
            writer.RawListings.Add(listing);
            await writer.SaveChangesAsync();
        }

        await using var reader = fixture.CreateContext();
        var loaded = await reader.RawListings.AsNoTracking().SingleAsync(r => r.Id == listing.Id);

        Assert.Null(loaded.Parsed);
        Assert.Null(loaded.Extracted);
        Assert.Null(loaded.BursaryId);
    }

    [Fact]
    public async Task RawListing_Duplicate_SourceId_And_Url_Throws()
    {
        var source = new Source { Key = UniqueKey(), Name = "Dup Source", BaseUrl = "https://dup.test" };
        var url = "https://dup.test/same-" + Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        await using (var first = fixture.CreateContext())
        {
            first.RawListings.Add(NewRawListing(source, url, now));
            await first.SaveChangesAsync();
        }

        await using var second = fixture.CreateContext();
        second.RawListings.Add(NewRawListing(source, url, now));
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task IngestionRun_With_Source_Counters_Saves_And_Reloads()
    {
        var source = new Source { Key = UniqueKey(), Name = "Run Source", BaseUrl = "https://run.test" };
        var started = new DateTimeOffset(2026, 5, 2, 6, 0, 0, TimeSpan.Zero);
        var run = new IngestionRun
        {
            Trigger = IngestionTrigger.Scheduled,
            Status = RunStatus.PartiallyFailed,
            StartedAt = started,
            FinishedAt = started.AddMinutes(12),
        };
        run.Sources.Add(new IngestionRunSource
        {
            Run = run,
            Source = source,
            Discovered = 40,
            Fetched = 35,
            Unchanged = 20,
            Created = 10,
            Updated = 5,
            Failed = 5,
            ErrorSummary = "5 pages timed out",
        });

        await using (var writer = fixture.CreateContext())
        {
            writer.IngestionRuns.Add(run);
            await writer.SaveChangesAsync();
        }

        await using var reader = fixture.CreateContext();
        var loaded = await reader.IngestionRuns
            .AsNoTracking()
            .Include(r => r.Sources)
            .SingleAsync(r => r.Id == run.Id);

        Assert.Equal(IngestionTrigger.Scheduled, loaded.Trigger);
        Assert.Equal(RunStatus.PartiallyFailed, loaded.Status);
        Assert.Equal(started, loaded.StartedAt);
        Assert.Equal(started.AddMinutes(12), loaded.FinishedAt);

        var counters = Assert.Single(loaded.Sources);
        Assert.Equal(source.Id, counters.SourceId);
        Assert.Equal(40, counters.Discovered);
        Assert.Equal(35, counters.Fetched);
        Assert.Equal(20, counters.Unchanged);
        Assert.Equal(10, counters.Created);
        Assert.Equal(5, counters.Updated);
        Assert.Equal(5, counters.Failed);
        Assert.Equal("5 pages timed out", counters.ErrorSummary);
    }

    [Fact]
    public async Task IngestionRunRequest_For_All_Sources_And_For_One_Source_Saves_And_Reloads()
    {
        var source = new Source { Key = UniqueKey(), Name = "Request Source", BaseUrl = "https://request.test" };
        var userId = Guid.CreateVersion7();
        var requestedAt = new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero);
        var allSources = new IngestionRunRequest { RequestedByUserId = userId, RequestedAt = requestedAt };
        var oneSource = new IngestionRunRequest
        {
            SourceId = source.Id,
            Source = source,
            RequestedByUserId = userId,
            RequestedAt = requestedAt,
            PickedUpAt = requestedAt.AddMinutes(1),
        };

        await using (var writer = fixture.CreateContext())
        {
            writer.IngestionRunRequests.AddRange(allSources, oneSource);
            await writer.SaveChangesAsync();
        }

        await using var reader = fixture.CreateContext();
        var loadedAll = await reader.IngestionRunRequests.AsNoTracking().SingleAsync(r => r.Id == allSources.Id);
        var loadedOne = await reader.IngestionRunRequests.AsNoTracking().SingleAsync(r => r.Id == oneSource.Id);

        Assert.Null(loadedAll.SourceId);
        Assert.Equal(userId, loadedAll.RequestedByUserId);
        Assert.Equal(requestedAt, loadedAll.RequestedAt);
        Assert.Null(loadedAll.PickedUpAt);

        Assert.Equal(source.Id, loadedOne.SourceId);
        Assert.Equal(requestedAt.AddMinutes(1), loadedOne.PickedUpAt);
    }

    private static RawListing NewRawListing(Source source, string url, DateTimeOffset now) => new()
    {
        Source = source,
        Url = url,
        ContentHash = new string('c', 64),
        Html = "<p>dup</p>",
        FirstFetchedAt = now,
        LastFetchedAt = now,
        LastChangedAt = now,
    };

    private static string UniqueKey() => "k" + Guid.NewGuid().ToString("N");
}
