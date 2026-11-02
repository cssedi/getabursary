using GetABursary.Core.Common;

namespace GetABursary.Worker.Sources.Http;

public static class FetchErrors
{
    public static Error DisallowedByRobots(Uri url) =>
        new("Fetch.DisallowedByRobots", $"robots.txt disallows fetching {url}", ErrorType.Forbidden);

    public static Error NotFound(Uri url) =>
        new("Fetch.NotFound", $"{url} returned 404", ErrorType.NotFound);

    public static Error TooLarge(Uri url, int maxBytes) =>
        new("Fetch.TooLarge", $"{url} exceeds the {maxBytes} byte limit", ErrorType.Validation);

    public static Error HttpStatus(Uri url, int statusCode) =>
        new("Fetch.HttpError", $"{url} returned HTTP {statusCode}", ErrorType.Unexpected);

    public static Error Network(Uri url, string reason) =>
        new("Fetch.NetworkError", $"Fetching {url} failed: {reason}", ErrorType.Unexpected);
}
