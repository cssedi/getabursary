using GetABursary.Core.Common;
using GetABursary.Worker.Sources.Http;

namespace GetABursary.Worker.Sources;

public sealed record SourceOptions(Uri BaseUrl, int CrawlDelayMs, int MaxPages = 20, int MaxListings = 500);

/// <summary>Implemented once per aggregator site.</summary>
public interface ISourceAdapter
{
    /// <summary>Matches <c>source.key</c>, e.g. <c>zabursaries</c>.</summary>
    string SourceKey { get; }

    /// <summary>Walks the site's index pages and yields listing URLs.</summary>
    IAsyncEnumerable<Uri> DiscoverAsync(IPageFetcher fetcher, SourceOptions options, CancellationToken ct);

    /// <summary>Parses one listing page. Pure: no I/O.</summary>
    Result<RawListing> Parse(Uri url, string html);
}
