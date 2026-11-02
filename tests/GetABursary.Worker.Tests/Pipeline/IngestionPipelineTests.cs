using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Worker.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Worker.Tests.Pipeline;

[Collection("Postgres")]
public class IngestionPipelineTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 1, 3, 0, 0, TimeSpan.Zero);

    private static string Page(string title) => $"<html><body><h1>{title}</h1><p>Engineering bursary</p></body></html>";

    [Fact]
    public async Task RunAsync_OneSourceThrows_GoodSourceIngestedBadErrorRecordedRunPartiallyFailed()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var goodUrls = new[] { new Uri($"https://good-{tag}.test/a"), new Uri($"https://good-{tag}.test/b") };
        using var harness = new PipelineHarness(
            fixture,
            T0,
            [new FakeAdapter($"good-{tag}", goodUrls), new FakeAdapter($"bad-{tag}", [], throwOnDiscover: true)]);
        await harness.DisableAllSourcesAsync();
        var goodId = await harness.CreateSourceAsync($"good-{tag}");
        var badId = await harness.CreateSourceAsync($"bad-{tag}");
        harness.Fetcher.Pages[goodUrls[0]] = Page("Alpha Mining Engineering Award");
        harness.Fetcher.Pages[goodUrls[1]] = Page("Zulu Chartered Accounting Scheme");

        var runId = await harness.RunAsync();

        var run = await harness.GetRunAsync(runId);
        Assert.Equal(RunStatus.PartiallyFailed, run.Status);
        var good = run.Sources.Single(s => s.SourceId == goodId);
        Assert.Equal(2, good.Discovered);
        Assert.Equal(2, good.Fetched);
        Assert.Equal(2, good.Created);
        Assert.Equal(0, good.Failed);
        Assert.Null(good.ErrorSummary);
        var bad = run.Sources.Single(s => s.SourceId == badId);
        Assert.Contains("index page exploded", bad.ErrorSummary);

        await using var db = fixture.CreateContext();
        var raws = await db.RawListings.AsNoTracking().Where(r => r.SourceId == goodId).ToListAsync();
        Assert.Equal(2, raws.Count);
        Assert.All(raws, r =>
        {
            Assert.NotNull(r.BursaryId);
            Assert.NotNull(r.Parsed);
            Assert.NotNull(r.Extracted);
        });
        Assert.Equal(2, await db.Bursaries.CountAsync(b => raws.Select(r => r.BursaryId).Contains(b.Id)));
    }

    [Fact]
    public async Task RunAsync_SecondRunWithIdenticalHtml_AllUnchangedNoNewBursariesLastSeenUpdated()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var urls = new[] { new Uri($"https://same-{tag}.test/a"), new Uri($"https://same-{tag}.test/b") };
        using var harness = new PipelineHarness(fixture, T0, [new FakeAdapter($"same-{tag}", urls)]);
        await harness.DisableAllSourcesAsync();
        var sourceId = await harness.CreateSourceAsync($"same-{tag}");
        harness.Fetcher.Pages[urls[0]] = Page("Bravo Medical Sciences Fund");
        harness.Fetcher.Pages[urls[1]] = Page("Yankee Actuarial Trust");

        await harness.RunAsync();
        await using var before = fixture.CreateContext();
        var bursaryIds = await before.RawListings.AsNoTracking().Where(r => r.SourceId == sourceId).Select(r => r.BursaryId!.Value).ToListAsync();
        var bursaryCount = await before.Bursaries.CountAsync();

        harness.Clock.UtcNow = T0.AddDays(1);
        var runId = await harness.RunAsync();

        var row = (await harness.GetRunAsync(runId)).Sources.Single(s => s.SourceId == sourceId);
        Assert.Equal(2, row.Unchanged);
        Assert.Equal(0, row.Created);
        Assert.Equal(0, row.Updated);
        Assert.Equal(0, row.Failed);

        await using var db = fixture.CreateContext();
        Assert.Equal(bursaryCount, await db.Bursaries.CountAsync());
        var links = await db.BursarySources.AsNoTracking().Where(s => s.SourceId == sourceId).ToListAsync();
        Assert.Equal(2, links.Count);
        Assert.All(links, l => Assert.Equal(T0.AddDays(1), l.LastSeenAt));
        Assert.Equal(bursaryIds.Order(), links.Select(l => l.BursaryId).Order());
    }

    [Fact]
    public async Task RunAsync_ChangedHtml_UpdatesExistingBursary()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var url = new Uri($"https://chg-{tag}.test/a");
        using var harness = new PipelineHarness(fixture, T0, [new FakeAdapter($"chg-{tag}", [url])]);
        await harness.DisableAllSourcesAsync();
        var sourceId = await harness.CreateSourceAsync($"chg-{tag}");
        harness.Fetcher.Pages[url] = Page("Charlie Geology Bursary");
        await harness.RunAsync();

        harness.Fetcher.Pages[url] = Page("Charlie Geology Bursary") + "<!-- more -->".Replace("more", "<p>new text</p>");
        harness.Clock.UtcNow = T0.AddDays(1);
        var runId = await harness.RunAsync();

        var row = (await harness.GetRunAsync(runId)).Sources.Single(s => s.SourceId == sourceId);
        Assert.Equal(1, row.Updated);
        Assert.Equal(0, row.Created);
    }

    [Fact]
    public async Task RunAsync_ExtractionFails_RaisesLowConfidenceReviewItemAndCountsFailed()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var url = new Uri($"https://ext-{tag}.test/a");
        using var harness = new PipelineHarness(fixture, T0, [new FakeAdapter($"ext-{tag}", [url])], new FailingExtractor());
        await harness.DisableAllSourcesAsync();
        var sourceId = await harness.CreateSourceAsync($"ext-{tag}");
        harness.Fetcher.Pages[url] = Page("Delta Failing Extraction Bursary");

        var runId = await harness.RunAsync();

        var run = await harness.GetRunAsync(runId);
        Assert.Equal(1, run.Sources.Single().Failed);
        Assert.Null(run.Sources.Single().ErrorSummary); // a failed listing does not fail its source
        await using var db = fixture.CreateContext();
        var raw = await db.RawListings.AsNoTracking().SingleAsync(r => r.SourceId == sourceId);
        Assert.Null(raw.BursaryId);
        Assert.NotNull(raw.Parsed);
        var item = await db.ReviewItems.AsNoTracking().SingleAsync(r => r.RawListingId == raw.Id);
        Assert.Equal(ReviewItemType.LowConfidence, item.Type);
        Assert.Contains("model unavailable", item.Details!.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task RunAsync_ListingFetchFails_CountsFailedAndContinues()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var urls = new[] { new Uri($"https://fetch-{tag}.test/missing"), new Uri($"https://fetch-{tag}.test/ok") };
        using var harness = new PipelineHarness(fixture, T0, [new FakeAdapter($"fetch-{tag}", urls)]);
        await harness.DisableAllSourcesAsync();
        var sourceId = await harness.CreateSourceAsync($"fetch-{tag}");
        harness.Fetcher.Pages[urls[1]] = Page("Echo Surveying Bursary");

        var runId = await harness.RunAsync();

        var row = (await harness.GetRunAsync(runId)).Sources.Single(s => s.SourceId == sourceId);
        Assert.Equal(2, row.Discovered);
        Assert.Equal(1, row.Fetched);
        Assert.Equal(1, row.Failed);
        Assert.Equal(1, row.Created);
    }

    [Fact]
    public async Task RunAsync_NoAdapterForSource_CountsSourceFailedAndRunFailedWhenNoneSucceeded()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        using var harness = new PipelineHarness(fixture, T0, []);
        await harness.DisableAllSourcesAsync();
        var sourceId = await harness.CreateSourceAsync($"orphan-{tag}");

        var runId = await harness.RunAsync();

        var run = await harness.GetRunAsync(runId);
        Assert.Equal(RunStatus.Failed, run.Status);
        Assert.Contains($"orphan-{tag}", run.Sources.Single(s => s.SourceId == sourceId).ErrorSummary);
    }

    [Fact]
    public async Task RunAsync_SourceIdGiven_OnlyThatSourceRuns()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var urlA = new Uri($"https://one-{tag}.test/a");
        var urlB = new Uri($"https://two-{tag}.test/a");
        using var harness = new PipelineHarness(
            fixture, T0, [new FakeAdapter($"one-{tag}", [urlA]), new FakeAdapter($"two-{tag}", [urlB])]);
        await harness.DisableAllSourcesAsync();
        var oneId = await harness.CreateSourceAsync($"one-{tag}");
        await harness.CreateSourceAsync($"two-{tag}");
        harness.Fetcher.Pages[urlA] = Page("Foxtrot Physics Bursary");
        harness.Fetcher.Pages[urlB] = Page("Golf Law Bursary");

        var runId = await harness.RunAsync(oneId);

        Assert.Equal(oneId, (await harness.GetRunAsync(runId)).Sources.Single().SourceId);
    }
}
