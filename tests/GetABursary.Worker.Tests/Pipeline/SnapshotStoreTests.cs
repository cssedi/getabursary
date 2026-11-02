using GetABursary.Core.Common;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Repositories.Ingestion;
using GetABursary.Worker.Pipeline;
using GetABursary.Worker.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Worker.Tests.Pipeline;

[Collection("Postgres")]
public class SnapshotStoreTests(PostgresFixture fixture)
{
    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }

    private const string Page = "<html><body><h1>Bursary</h1><p>Closes 31 March 2027</p><script>var a = 1;</script></body></html>";

    private static readonly DateTimeOffset T0 = new(2026, 4, 1, 3, 0, 0, TimeSpan.Zero);

    private async Task<Guid> CreateSourceAsync()
    {
        await using var db = fixture.CreateContext();
        var source = new Source { Key = "src-" + Guid.NewGuid().ToString("N"), Name = "Snap Source", BaseUrl = "https://snap.test" };
        db.Sources.Add(source);
        await db.SaveChangesAsync();
        return source.Id;
    }

    private async Task<SnapshotResult> SaveAsync(Guid sourceId, Uri url, string html, DateTimeOffset now)
    {
        await using var db = fixture.CreateContext();
        var store = new SnapshotStore(new RawListingRepository(db), new FakeClock(now));
        return await store.SaveAsync(sourceId, url, html, CancellationToken.None);
    }

    [Fact]
    public async Task SaveAsync_FirstSave_IsChangedAndPersistsSnapshot()
    {
        var sourceId = await CreateSourceAsync();
        var url = new Uri("https://snap.test/a");

        var result = await SaveAsync(sourceId, url, Page, T0);

        Assert.True(result.Changed);
        await using var db = fixture.CreateContext();
        var row = await db.RawListings.AsNoTracking().SingleAsync(r => r.Id == result.RawListingId);
        Assert.Equal(Page, row.Html);
        Assert.Equal(64, row.ContentHash.Length);
        Assert.Equal(T0, row.FirstFetchedAt);
        Assert.Equal(T0, row.LastChangedAt);
        Assert.Equal(0, row.MissedRuns);
    }

    [Fact]
    public async Task SaveAsync_SameHtml_NotChangedButUpdatesLastFetched()
    {
        var sourceId = await CreateSourceAsync();
        var url = new Uri("https://snap.test/a");
        var first = await SaveAsync(sourceId, url, Page, T0);

        await using (var db = fixture.CreateContext())
        {
            await db.RawListings.Where(r => r.Id == first.RawListingId).ExecuteUpdateAsync(s => s.SetProperty(r => r.MissedRuns, 3));
        }

        var second = await SaveAsync(sourceId, url, Page, T0.AddDays(1));

        Assert.False(second.Changed);
        Assert.Equal(first.RawListingId, second.RawListingId);
        await using var reader = fixture.CreateContext();
        var row = await reader.RawListings.AsNoTracking().SingleAsync(r => r.Id == first.RawListingId);
        Assert.Equal(T0.AddDays(1), row.LastFetchedAt);
        Assert.Equal(T0, row.LastChangedAt);
        Assert.Equal(0, row.MissedRuns);
    }

    [Fact]
    public async Task SaveAsync_OnlyScriptAndWhitespaceDiffer_NotChanged()
    {
        var sourceId = await CreateSourceAsync();
        var url = new Uri("https://snap.test/a");
        await SaveAsync(sourceId, url, Page, T0);

        var noisy = "<html>\n  <body>\n <h1>Bursary</h1>   <p>Closes 31 March 2027</p>\n<script>var tracker = 'abc123';</script>\n</body></html>";
        var result = await SaveAsync(sourceId, url, noisy, T0.AddDays(1));

        Assert.False(result.Changed);
        await using var reader = fixture.CreateContext();
        var row = await reader.RawListings.AsNoTracking().SingleAsync(r => r.Id == result.RawListingId);
        Assert.Equal(Page, row.Html);
    }

    [Fact]
    public async Task SaveAsync_RealChange_IsChangedAndUpdatesHtml()
    {
        var sourceId = await CreateSourceAsync();
        var url = new Uri("https://snap.test/a");
        await SaveAsync(sourceId, url, Page, T0);

        var edited = Page.Replace("31 March 2027", "30 April 2027", StringComparison.Ordinal);
        var result = await SaveAsync(sourceId, url, edited, T0.AddDays(2));

        Assert.True(result.Changed);
        await using var reader = fixture.CreateContext();
        var row = await reader.RawListings.AsNoTracking().SingleAsync(r => r.Id == result.RawListingId);
        Assert.Equal(edited, row.Html);
        Assert.Equal(T0.AddDays(2), row.LastChangedAt);
        Assert.Equal(T0, row.FirstFetchedAt);
    }
}
