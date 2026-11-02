using GetABursary.Core.Common;

namespace GetABursary.Worker.Sources.Http;

/// <summary>A successfully fetched page.</summary>
public sealed record FetchedPage(Uri Url, int StatusCode, string Html, DateTimeOffset FetchedAt);

/// <summary>The one shared, polite way for source adapters to fetch pages.</summary>
public interface IPageFetcher
{
    Task<Result<FetchedPage>> GetAsync(Uri url, int minDelayMs, CancellationToken ct);
}
