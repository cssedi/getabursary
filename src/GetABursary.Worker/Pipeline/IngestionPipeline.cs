using System.Text.Json;
using System.Text.Json.Serialization;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;
using GetABursary.Core.Domain.Reports;
using GetABursary.Core.Repositories.Ingestion;
using GetABursary.Core.Repositories.Reports;
using GetABursary.Worker.Extraction;
using GetABursary.Worker.Pipeline.Jobs;
using GetABursary.Worker.Pipeline.Merging;
using GetABursary.Worker.Sources;
using GetABursary.Worker.Sources.Http;
using Microsoft.Extensions.Logging;

namespace GetABursary.Worker.Pipeline;

/// <summary>Discover → Fetch → Snapshot → Parse → Extract → Merge for every enabled source (spec 01 §3.2).</summary>
public sealed partial class IngestionPipeline(
    IIngestionSourceRepository sources,
    SourceAdapterRegistry adapters,
    IClosureJob closureJob,
    IVanishedJob vanishedJob,
    IServiceScopeFactory scopeFactory,
    IClock clock,
    ILogger<IngestionPipeline> logger) : IIngestionPipeline
{
    public const int MaxErrorSummaryLength = 2000;

    private static readonly JsonSerializerOptions StoredJson = new() { Converters = { new JsonStringEnumConverter() } };

    private enum ListingOutcome
    {
        Unchanged,
        Created,
        Updated,
        Failed,
    }

    public async Task RunAsync(Guid runId, Guid? sourceId, CancellationToken ct)
    {
        var enabled = await sources.GetEnabledSourcesAsync(sourceId, ct);
        var succeeded = 0;
        var failed = 0;

        foreach (var source in enabled)
        {
            var row = new IngestionRunSource { RunId = runId, SourceId = source.Id };
            var seen = new List<Guid>();
            try
            {
                await ProcessSourceAsync(source, row, seen, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                row.ErrorSummary = Summarise(ex);
                LogSourceFailed(logger, ex, source.Key);
            }

            if (row.ErrorSummary is null)
            {
                succeeded++;
                await RunVanishedAsync(source, seen, ct);
            }
            else
            {
                failed++;
            }

            sources.AddRunSource(row);
            await sources.SaveChangesAsync(ct);
        }

        await RunClosureAsync(ct);

        if (failed > 0)
        {
            var status = succeeded > 0 ? RunStatus.PartiallyFailed : RunStatus.Failed;
            await sources.SetRunStatusAsync(runId, status, ct);
            await sources.SaveChangesAsync(ct);
        }
    }

    private async Task ProcessSourceAsync(Source source, IngestionRunSource row, List<Guid> seen, CancellationToken ct)
    {
        var lookup = adapters.Get(source.Key);
        if (!lookup.IsSuccess)
        {
            row.ErrorSummary = Truncate(lookup.Errors[0].Message);
            LogNoAdapter(logger, source.Key);
            return;
        }

        var adapter = lookup.Value!;
        await using var sourceScope = scopeFactory.CreateAsyncScope();
        var fetcher = sourceScope.ServiceProvider.GetRequiredService<IPageFetcher>();
        var options = new SourceOptions(new Uri(source.BaseUrl), source.CrawlDelayMs);

        await foreach (var url in adapter.DiscoverAsync(fetcher, options, ct).WithCancellation(ct))
        {
            row.Discovered++;
            try
            {
                switch (await ProcessListingAsync(source, adapter, fetcher, url, row, seen, ct))
                {
                    case ListingOutcome.Unchanged: row.Unchanged++; break;
                    case ListingOutcome.Created: row.Created++; break;
                    case ListingOutcome.Updated: row.Updated++; break;
                    default: row.Failed++; break;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                row.Failed++;
                LogListingFailed(logger, ex, url);
            }
        }
    }

    private async Task<ListingOutcome> ProcessListingAsync(
        Source source, ISourceAdapter adapter, IPageFetcher fetcher, Uri url, IngestionRunSource row, List<Guid> seen, CancellationToken ct)
    {
        var fetched = await fetcher.GetAsync(url, source.CrawlDelayMs, ct);
        if (!fetched.IsSuccess)
        {
            LogFetchFailed(logger, url, fetched.Errors[0].Message);
            return ListingOutcome.Failed;
        }

        row.Fetched++;
        var html = fetched.Value!.Html;

        // A scope per listing keeps a failed listing's half-tracked entities away from the next one.
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var rawListings = services.GetRequiredService<IRawListingRepository>();

        var snapshot = await services.GetRequiredService<ISnapshotStore>().SaveAsync(source.Id, url, html, ct);
        seen.Add(snapshot.RawListingId);

        if (!snapshot.Changed)
        {
            await rawListings.MarkSeenAsync(snapshot.RawListingId, clock.UtcNow, ct);
            return ListingOutcome.Unchanged;
        }

        var stored = await rawListings.GetByIdAsync(snapshot.RawListingId, ct)
            ?? throw new InvalidOperationException($"Raw listing {snapshot.RawListingId} disappeared after snapshot.");

        var parsed = adapter.Parse(url, html);
        if (!parsed.IsSuccess)
        {
            LogParseFailed(logger, url, parsed.Errors[0].Message);
            return ListingOutcome.Failed;
        }

        var listing = parsed.Value!;
        stored.Parsed = JsonSerializer.SerializeToDocument(listing, StoredJson);

        var extracted = await services.GetRequiredService<IEligibilityExtractor>().ExtractAsync(listing, ct);
        if (!extracted.IsSuccess)
        {
            var error = extracted.Errors[0];
            services.GetRequiredService<IReviewItemRepository>().Add(new ReviewItem
            {
                Type = ReviewItemType.LowConfidence,
                RawListingId = stored.Id,
                BursaryId = stored.BursaryId,
                Details = JsonSerializer.SerializeToDocument(new { error = error.Message, code = error.Code }),
            });
            await rawListings.SaveChangesAsync(ct);
            LogExtractionFailed(logger, url, error.Message);
            return ListingOutcome.Failed;
        }

        stored.Extracted = JsonSerializer.SerializeToDocument(extracted.Value!, StoredJson);

        var outcome = await services.GetRequiredService<IBursaryMerger>()
            .MergeAsync(source.Id, url, stored.Id, listing, extracted.Value!, ct);
        return outcome.Kind == MergeKind.Merged ? ListingOutcome.Updated : ListingOutcome.Created;
    }

    private async Task RunVanishedAsync(Source source, IReadOnlyCollection<Guid> seen, CancellationToken ct)
    {
        try
        {
            await vanishedJob.RunAsync(source.Id, seen, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogJobFailed(logger, ex, nameof(VanishedJob));
        }
    }

    private async Task RunClosureAsync(CancellationToken ct)
    {
        try
        {
            await closureJob.RunAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogJobFailed(logger, ex, nameof(ClosureJob));
        }
    }

    private static string Summarise(Exception ex) => Truncate($"{ex.GetType().Name}: {ex.Message}");

    private static string Truncate(string text) =>
        text.Length <= MaxErrorSummaryLength ? text : text[..MaxErrorSummaryLength];

    [LoggerMessage(Level = LogLevel.Error, Message = "Ingestion of source {SourceKey} failed")]
    private static partial void LogSourceFailed(ILogger logger, Exception ex, string sourceKey);

    [LoggerMessage(Level = LogLevel.Error, Message = "No source adapter is registered for source {SourceKey}")]
    private static partial void LogNoAdapter(ILogger logger, string sourceKey);

    [LoggerMessage(Level = LogLevel.Error, Message = "Ingesting listing {Url} failed")]
    private static partial void LogListingFailed(ILogger logger, Exception ex, Uri url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Fetching {Url} failed: {Reason}")]
    private static partial void LogFetchFailed(ILogger logger, Uri url, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Parsing {Url} failed: {Reason}")]
    private static partial void LogParseFailed(ILogger logger, Uri url, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Extraction for {Url} failed: {Reason}")]
    private static partial void LogExtractionFailed(ILogger logger, Uri url, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Job} failed")]
    private static partial void LogJobFailed(ILogger logger, Exception ex, string job);
}
