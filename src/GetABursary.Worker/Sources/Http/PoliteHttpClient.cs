using System.Net;
using System.Net.Http.Headers;
using GetABursary.Core.Common;
using Microsoft.Extensions.Configuration;

namespace GetABursary.Worker.Sources.Http;

/// <summary>
/// Fetches pages while obeying robots.txt, spacing requests per host and identifying itself honestly.
/// Retries are provided by the resilience handler on the underlying <see cref="HttpClient"/>.
/// </summary>
public sealed class PoliteHttpClient(
    HttpClient httpClient,
    IConfiguration configuration,
    IClock clock,
    IDelayer delayer,
    PoliteHostState state) : IPageFetcher
{
    public const string DefaultUserAgent = "GetABursaryBot/1.0 (+https://getabursary.co.za/bot)";
    public const int MaxResponseBytes = 2 * 1024 * 1024;
    private static readonly TimeSpan RobotsCacheDuration = TimeSpan.FromHours(6);

    private readonly string _userAgent = configuration["Ingestion:UserAgent"] is { Length: > 0 } ua ? ua : DefaultUserAgent;

    public async Task<Result<FetchedPage>> GetAsync(Uri url, int minDelayMs, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(url);
        var host = state.For(url);

        var rules = await GetRulesAsync(url, host, ct);
        if (!rules.IsAllowed(url.PathAndQuery))
        {
            return FetchErrors.DisallowedByRobots(url);
        }

        var minDelay = TimeSpan.FromMilliseconds(Math.Max(0, minDelayMs));
        var delay = rules.CrawlDelay is { } crawl && crawl > minDelay ? crawl : minDelay;

        using var response = await SendAsync(url, host, delay, ct);
        if (response.Error is not null)
        {
            return response.Error;
        }

        var message = response.Message!;
        if (message.StatusCode == HttpStatusCode.NotFound)
        {
            return FetchErrors.NotFound(url);
        }

        if (!message.IsSuccessStatusCode)
        {
            return FetchErrors.HttpStatus(url, (int)message.StatusCode);
        }

        if (message.Content.Headers.ContentLength > MaxResponseBytes)
        {
            return FetchErrors.TooLarge(url, MaxResponseBytes);
        }

        try
        {
            var bytes = await ReadLimitedAsync(message.Content, ct);
            if (bytes is null)
            {
                return FetchErrors.TooLarge(url, MaxResponseBytes);
            }

            var charset = message.Content.Headers.ContentType?.CharSet;
            var encoding = System.Text.Encoding.UTF8;
            if (charset is not null)
            {
                try
                {
                    encoding = System.Text.Encoding.GetEncoding(charset.Trim('"'));
                }
                catch (ArgumentException)
                {
                    // Unknown charset: fall back to UTF-8.
                }
            }

            return new FetchedPage(url, (int)message.StatusCode, encoding.GetString(bytes), clock.UtcNow);
        }
        catch (HttpRequestException ex)
        {
            return FetchErrors.Network(url, ex.Message);
        }
    }

    private static async Task<byte[]?> ReadLimitedAsync(HttpContent content, CancellationToken ct)
    {
        await using var stream = await content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > MaxResponseBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private async Task<RobotsRules> GetRulesAsync(Uri url, PoliteHostState.HostEntry host, CancellationToken ct)
    {
        await host.RobotsLock.WaitAsync(ct);
        try
        {
            if (host.Rules is not null && clock.UtcNow < host.RulesExpiry)
            {
                return host.Rules;
            }

            host.Rules = await LoadRobotsAsync(url, host, ct);
            host.RulesExpiry = clock.UtcNow + RobotsCacheDuration;
            return host.Rules;
        }
        finally
        {
            host.RobotsLock.Release();
        }
    }

    private async Task<RobotsRules> LoadRobotsAsync(Uri url, PoliteHostState.HostEntry host, CancellationToken ct)
    {
        var robotsUrl = new Uri(url, "/robots.txt");
        using var response = await SendAsync(robotsUrl, host, TimeSpan.Zero, ct);

        // Network failure/timeout, 5xx (after retries) or any unexpected status: disallow for this run.
        if (response.Message is not { } message)
        {
            return RobotsRules.DisallowAll;
        }

        if (message.StatusCode == HttpStatusCode.NotFound)
        {
            return RobotsRules.AllowAll;
        }

        if (!message.IsSuccessStatusCode)
        {
            return RobotsRules.DisallowAll;
        }

        try
        {
            var bytes = await ReadLimitedAsync(message.Content, ct);
            if (bytes is null)
            {
                return RobotsRules.DisallowAll;
            }

            return RobotsTxtParser.Parse(System.Text.Encoding.UTF8.GetString(bytes), ProductToken());
        }
        catch (HttpRequestException)
        {
            return RobotsRules.DisallowAll;
        }
    }

    /// <summary>Waits out the per-host delay, then sends the request. Request starts to a host are serialised.</summary>
    private async Task<SendOutcome> SendAsync(Uri url, PoliteHostState.HostEntry host, TimeSpan delay, CancellationToken ct)
    {
        await host.Throttle.WaitAsync(ct);
        try
        {
            if (host.LastRequestStart is { } last && delay > TimeSpan.Zero)
            {
                var wait = delay - (clock.UtcNow - last);
                if (wait > TimeSpan.Zero)
                {
                    await delayer.DelayAsync(wait, ct);
                }
            }

            host.LastRequestStart = clock.UtcNow;
        }
        finally
        {
            host.Throttle.Release();
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.Clear();
        request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain", 0.9));

        try
        {
            var message = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            return new SendOutcome(message, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException
            && !ct.IsCancellationRequested)
        {
            return new SendOutcome(null, FetchErrors.Network(url, ex.Message));
        }
    }

    private string ProductToken()
    {
        var token = _userAgent.Split(['/', ' '], 2)[0];
        return token.Length > 0 ? token : "GetABursaryBot";
    }

    private sealed record SendOutcome(HttpResponseMessage? Message, Error? Error) : IDisposable
    {
        public void Dispose() => Message?.Dispose();
    }
}
