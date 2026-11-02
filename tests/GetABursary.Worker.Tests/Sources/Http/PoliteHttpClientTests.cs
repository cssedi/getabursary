using System.Net;
using System.Text;
using GetABursary.Core.Common;
using GetABursary.Worker.Sources.Http;
using Microsoft.Extensions.Configuration;

namespace GetABursary.Worker.Tests.Sources.Http;

public class PoliteHttpClientTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        public Dictionary<string, Func<HttpResponseMessage>> Routes { get; } = new();

        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> Urls { get; } = [];

        public List<string> UserAgents { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Urls.Add(request.RequestUri!.ToString());
            UserAgents.Add(string.Join(' ', request.Headers.GetValues("User-Agent")));
            return Task.FromResult(Routes.TryGetValue(request.RequestUri.ToString(), out var make)
                ? make()
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 2, 8, 0, 0, TimeSpan.Zero);

        public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }

    private sealed class RecordingDelayer : IDelayer
    {
        public List<TimeSpan> Delays { get; } = [];

        public Task DelayAsync(TimeSpan delay, CancellationToken ct)
        {
            Delays.Add(delay);
            return Task.CompletedTask;
        }
    }

    private static HttpResponseMessage Text(string body, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "text/html") };

    private static (PoliteHttpClient Client, FakeHandler Handler, RecordingDelayer Delayer) Create(string? userAgent = null)
    {
        var handler = new FakeHandler();
        var delayer = new RecordingDelayer();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(userAgent is null ? [] : new Dictionary<string, string?> { ["Ingestion:UserAgent"] = userAgent })
            .Build();
        var client = new PoliteHttpClient(new HttpClient(handler), config, new FakeClock(), delayer, new PoliteHostState());
        return (client, handler, delayer);
    }

    [Fact]
    public async Task GetAsync_DisallowedUrl_IsNeverRequested()
    {
        var (client, handler, _) = Create();
        handler.Routes["https://a.example/robots.txt"] = () => Text("User-agent: *\nDisallow: /private\n");
        handler.Routes["https://a.example/private/x"] = () => Text("secret");

        var result = await client.GetAsync(new Uri("https://a.example/private/x"), 0, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Fetch.DisallowedByRobots", result.Errors[0].Code);
        Assert.DoesNotContain("https://a.example/private/x", handler.Urls);
    }

    [Fact]
    public async Task GetAsync_RobotsMissing_AllowsFetch()
    {
        var (client, handler, _) = Create();
        handler.Routes["https://a.example/page"] = () => Text("<html>hi</html>");

        var result = await client.GetAsync(new Uri("https://a.example/page"), 0, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("<html>hi</html>", result.Value.Html);
        Assert.Equal(200, result.Value.StatusCode);
    }

    [Fact]
    public async Task GetAsync_RobotsServerError_DisallowsFetch()
    {
        var (client, handler, _) = Create();
        handler.Routes["https://a.example/robots.txt"] = () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        handler.Routes["https://a.example/page"] = () => Text("x");

        var result = await client.GetAsync(new Uri("https://a.example/page"), 0, CancellationToken.None);

        Assert.Equal("Fetch.DisallowedByRobots", result.Errors[0].Code);
        Assert.DoesNotContain("https://a.example/page", handler.Urls);
    }

    [Fact]
    public async Task GetAsync_RobotsFetchedOncePerHost()
    {
        var (client, handler, _) = Create();
        handler.Routes["https://a.example/1"] = () => Text("1");
        handler.Routes["https://a.example/2"] = () => Text("2");

        await client.GetAsync(new Uri("https://a.example/1"), 0, CancellationToken.None);
        await client.GetAsync(new Uri("https://a.example/2"), 0, CancellationToken.None);

        Assert.Single(handler.Urls, u => u == "https://a.example/robots.txt");
    }

    [Fact]
    public async Task GetAsync_SameHost_RequestsMinDelayFromDelayer()
    {
        var (client, handler, delayer) = Create();
        handler.Routes["https://a.example/1"] = () => Text("1");
        handler.Routes["https://a.example/2"] = () => Text("2");

        await client.GetAsync(new Uri("https://a.example/1"), 1500, CancellationToken.None);
        await client.GetAsync(new Uri("https://a.example/2"), 1500, CancellationToken.None);

        Assert.Equal([TimeSpan.FromMilliseconds(1500), TimeSpan.FromMilliseconds(1500)], delayer.Delays);
    }

    [Fact]
    public async Task GetAsync_CrawlDelayLargerThanMin_UsesCrawlDelay()
    {
        var (client, handler, delayer) = Create();
        handler.Routes["https://a.example/robots.txt"] = () => Text("User-agent: *\nCrawl-delay: 5\n");
        handler.Routes["https://a.example/1"] = () => Text("1");

        await client.GetAsync(new Uri("https://a.example/1"), 1000, CancellationToken.None);

        Assert.Equal([TimeSpan.FromSeconds(5)], delayer.Delays);
    }

    [Fact]
    public async Task GetAsync_DifferentHosts_DoNotDelayEachOther()
    {
        var (client, handler, delayer) = Create();
        handler.Routes["https://a.example/1"] = () => Text("1");
        handler.Routes["https://b.example/1"] = () => Text("1");

        await client.GetAsync(new Uri("https://a.example/1"), 2000, CancellationToken.None);
        await client.GetAsync(new Uri("https://b.example/1"), 2000, CancellationToken.None);

        // Each host waits only after its own robots.txt request (one delay per host), never for the other host.
        Assert.Equal([TimeSpan.FromMilliseconds(2000), TimeSpan.FromMilliseconds(2000)], delayer.Delays);
    }

    [Fact]
    public async Task GetAsync_SendsConfiguredUserAgent()
    {
        var (client, handler, _) = Create("TestBot/2.0 (+https://example.test/bot)");
        handler.Routes["https://a.example/1"] = () => Text("1");

        await client.GetAsync(new Uri("https://a.example/1"), 0, CancellationToken.None);

        Assert.All(handler.UserAgents, ua => Assert.Equal("TestBot/2.0 (+https://example.test/bot)", ua));
        Assert.Equal(2, handler.UserAgents.Count);
    }

    [Fact]
    public async Task GetAsync_NoConfig_SendsDefaultUserAgent()
    {
        var (client, handler, _) = Create();
        handler.Routes["https://a.example/1"] = () => Text("1");

        await client.GetAsync(new Uri("https://a.example/1"), 0, CancellationToken.None);

        Assert.All(handler.UserAgents, ua => Assert.Equal(PoliteHttpClient.DefaultUserAgent, ua));
    }

    [Fact]
    public async Task GetAsync_NotFound_ReturnsFetchNotFound()
    {
        var (client, _, _) = Create();

        var result = await client.GetAsync(new Uri("https://a.example/missing"), 0, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Fetch.NotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task GetAsync_BodyOverTwoMegabytes_ReturnsTooLarge()
    {
        var (client, handler, _) = Create();
        handler.Routes["https://a.example/big"] = () => Text(new string('a', PoliteHttpClient.MaxResponseBytes + 1));

        var result = await client.GetAsync(new Uri("https://a.example/big"), 0, CancellationToken.None);

        Assert.Equal("Fetch.TooLarge", result.Errors[0].Code);
    }
}
