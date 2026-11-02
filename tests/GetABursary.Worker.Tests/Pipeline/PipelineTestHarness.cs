using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using GetABursary.Core.Common;
using GetABursary.Core.Data;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;
using GetABursary.Core.Repositories.Bursaries;
using GetABursary.Core.Repositories.Ingestion;
using GetABursary.Core.Repositories.Reports;
using GetABursary.Worker.Extraction;
using GetABursary.Worker.Pipeline;
using GetABursary.Worker.Pipeline.Jobs;
using GetABursary.Worker.Pipeline.Merging;
using GetABursary.Worker.Sources;
using GetABursary.Worker.Sources.Http;
using GetABursary.Worker.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SourceListing = GetABursary.Worker.Sources.RawListing;

namespace GetABursary.Worker.Tests.Pipeline;

internal sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;

    public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}

/// <summary>Serves canned HTML by URL; unknown URLs fail like a 404.</summary>
internal sealed class FakeFetcher : IPageFetcher
{
    public Dictionary<Uri, string> Pages { get; } = [];

    public Task<Result<FetchedPage>> GetAsync(Uri url, int minDelayMs, CancellationToken ct) =>
        Task.FromResult(Pages.TryGetValue(url, out var html)
            ? Result<FetchedPage>.Success(new FetchedPage(url, 200, html, DateTimeOffset.UnixEpoch))
            : Result<FetchedPage>.Failure(FetchErrors.NotFound(url)));
}

/// <summary>Discovers a fixed URL list (or throws) and parses <c>&lt;h1&gt;</c> as the title.</summary>
internal sealed partial class FakeAdapter(string key, IReadOnlyList<Uri> urls, bool throwOnDiscover = false) : ISourceAdapter
{
    public string SourceKey => key;

    public async IAsyncEnumerable<Uri> DiscoverAsync(
        IPageFetcher fetcher, SourceOptions options, [EnumeratorCancellation] CancellationToken ct)
    {
        await Task.Yield();
        if (throwOnDiscover)
        {
            throw new InvalidOperationException("index page exploded");
        }

        foreach (var url in urls)
        {
            yield return url;
        }
    }

    public Result<SourceListing> Parse(Uri url, string html)
    {
        var m = Heading().Match(html);
        return m.Success
            ? Result<SourceListing>.Success(new SourceListing(url, m.Groups[1].Value, null, null, null, null, "Open to all", null, null))
            : Result<SourceListing>.Failure(new Error("Parse.NoTitle", "no h1", ErrorType.Validation));
    }

    [GeneratedRegex("<h1>(.*?)</h1>")]
    private static partial Regex Heading();
}

internal sealed class FailingExtractor : IEligibilityExtractor
{
    public Task<Result<ExtractedEligibility>> ExtractAsync(SourceListing listing, CancellationToken ct) =>
        Task.FromResult(Result<ExtractedEligibility>.Failure(ExtractionErrors.Failed("model unavailable")));
}

internal sealed class PipelineHarness : IDisposable
{
    private readonly PostgresFixture _fixture;
    private readonly ServiceProvider _provider;

    public PipelineHarness(PostgresFixture fixture, DateTimeOffset now, IEnumerable<ISourceAdapter> adapters, IEligibilityExtractor? extractor = null)
    {
        _fixture = fixture;
        Clock = new FakeClock(now);
        Fetcher = new FakeFetcher();

        var services = new ServiceCollection();
        services.AddSingleton<IClock>(Clock);
        services.AddSingleton<IPageFetcher>(Fetcher);
        services.AddSingleton(new SourceAdapterRegistry(adapters));
        services.AddSingleton(extractor ?? new FakeEligibilityExtractor());
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        services.AddScoped(_ => fixture.CreateContext(Clock));
        services.AddScoped<IRawListingRepository, RawListingRepository>();
        services.AddScoped<IIngestionRunRepository, IngestionRunRepository>();
        services.AddScoped<ISnapshotStore, SnapshotStore>();
        services.AddBursaryMerging();
        services.AddIngestionPipeline();
        _provider = services.BuildServiceProvider();
    }

    public FakeClock Clock { get; }

    public FakeFetcher Fetcher { get; }

    /// <summary>Disables every existing source so that only this test's sources are ingested.</summary>
    public async Task DisableAllSourcesAsync()
    {
        await using var db = _fixture.CreateContext(Clock);
        await db.Sources.ExecuteUpdateAsync(s => s.SetProperty(x => x.Enabled, false));
    }

    public async Task<Guid> CreateSourceAsync(string key)
    {
        await using var db = _fixture.CreateContext(Clock);
        var source = new Source { Key = key, Name = key, BaseUrl = "https://" + key + ".test", Enabled = true, CrawlDelayMs = 0 };
        db.Sources.Add(source);
        await db.SaveChangesAsync();
        return source.Id;
    }

    /// <summary>Creates a run row, runs the pipeline in one scope (as <c>RunTrigger</c> does) and returns the run id.</summary>
    public async Task<Guid> RunAsync(Guid? sourceId = null)
    {
        await using var scope = _provider.CreateAsyncScope();
        var runs = scope.ServiceProvider.GetRequiredService<IIngestionRunRepository>();
        var run = await runs.CreateRunAsync(IngestionTrigger.Manual, Clock.UtcNow, CancellationToken.None);
        await runs.SaveChangesAsync(CancellationToken.None);

        await scope.ServiceProvider.GetRequiredService<IIngestionPipeline>().RunAsync(run.Id, sourceId, CancellationToken.None);
        return run.Id;
    }

    public async Task<IngestionRun> GetRunAsync(Guid runId)
    {
        await using var db = _fixture.CreateContext(Clock);
        return await db.IngestionRuns.AsNoTracking().Include(r => r.Sources).SingleAsync(r => r.Id == runId);
    }

    public void Dispose() => _provider.Dispose();
}
