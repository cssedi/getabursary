using System.Net;
using System.Text;
using System.Text.Json;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Worker.Extraction;
using GetABursary.Worker.Sources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GetABursary.Worker.Tests.Extraction;

public class LlmEligibilityExtractorTests
{
    private const string ApiKey = "test-key-DO-NOT-LOG-12345";

    private const string ToolUseResponse = """
        {"content":[{"type":"tool_use","name":"record_eligibility","input":{
          "fieldCodes":["08","99"],"allFieldsEligible":false,"qualificationNames":["BSc Eng"],
          "institutionShortNames":["UCT"],"studyLevels":["Undergraduate","Bogus"],"minAverage":150,
          "subjectRequirements":[{"subject":"Mathematics","minPercent":70}],
          "citizenship":"SouthAfrican","provinces":["Gauteng"],"coverage":["Tuition","Books"],
          "workBackRequired":true,"workBackDetails":"1 year","closingDate":"2027-02-01","openDate":null,
          "funderName":"Acme","summary":"A short summary.","confidence":0.9}}]}
        """;

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class FakeCatalog : IFieldCatalog
    {
        public Task<IReadOnlyDictionary<string, string>> GetCodesAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string> { ["08"] = "Engineering", ["04"] = "Business" });
    }

    private sealed class TestLogger : ILogger<LlmEligibilityExtractor>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception) + exception);
    }

    private static RawListing Listing(DateOnly? closing = null) =>
        new(new Uri("https://example.org/b"), "Eng bursary", null, closing, null, null, "Engineering students with 70% maths.", null, null);

    private static (LlmEligibilityExtractor Sut, StubHandler Handler, TestLogger Logger) Create(HttpStatusCode status, string body)
    {
        var handler = new StubHandler(status, body);
        var logger = new TestLogger();
        var options = Options.Create(new ExtractionOptions { Provider = "anthropic", ApiKey = ApiKey, Model = "claude-haiku-5-5" });
        return (new LlmEligibilityExtractor(new HttpClient(handler), new FakeCatalog(), options, logger), handler, logger);
    }

    [Fact]
    public async Task ExtractAsync_Request_HasHeadersModelAndForcedTool()
    {
        var (sut, handler, _) = Create(HttpStatusCode.OK, ToolUseResponse);

        await sut.ExtractAsync(Listing(), CancellationToken.None);

        Assert.Equal("https://api.anthropic.com/v1/messages", handler.Request!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal(ApiKey, handler.Request.Headers.GetValues("x-api-key").Single());
        Assert.Equal("2023-06-01", handler.Request.Headers.GetValues("anthropic-version").Single());

        using var doc = JsonDocument.Parse(handler.RequestBody!);
        var root = doc.RootElement;
        Assert.Equal("claude-haiku-5-5", root.GetProperty("model").GetString());
        Assert.Equal(1500, root.GetProperty("max_tokens").GetInt32());
        Assert.Equal(0, root.GetProperty("temperature").GetInt32());
        Assert.Equal("tool", root.GetProperty("tool_choice").GetProperty("type").GetString());
        Assert.Equal("record_eligibility", root.GetProperty("tool_choice").GetProperty("name").GetString());
        Assert.Equal("record_eligibility", root.GetProperty("tools")[0].GetProperty("name").GetString());
        var system = root.GetProperty("system").GetString()!;
        Assert.Contains("08 Engineering", system, StringComparison.Ordinal);
        Assert.Contains("Honours", system, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, handler.RequestBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractAsync_ToolUseResponse_MapsAndPostValidates()
    {
        var (sut, _, _) = Create(HttpStatusCode.OK, ToolUseResponse);

        var result = await sut.ExtractAsync(Listing(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var v = result.Value;
        Assert.Equal(["08"], v.FieldCodes);
        Assert.Equal(0.8m, v.Confidence);
        Assert.Equal(100m, v.MinAverage);
        Assert.Equal([StudyLevel.Undergraduate], v.StudyLevels);
        Assert.Equal(Citizenship.SouthAfrican, v.Citizenship);
        Assert.Equal([Province.Gauteng], v.Provinces);
        Assert.Equal([CoverageItem.Tuition, CoverageItem.Books], v.Coverage);
        Assert.Equal("Mathematics", v.SubjectRequirements.Single().Subject);
        Assert.True(v.WorkBackRequired);
        Assert.Equal(new DateOnly(2027, 2, 1), v.ClosingDate);
        Assert.Equal("Acme", v.FunderName);
    }

    [Fact]
    public async Task ExtractAsync_ParsedClosingDate_WinsOverModel()
    {
        var (sut, _, _) = Create(HttpStatusCode.OK, ToolUseResponse);
        var parsed = new DateOnly(2027, 3, 15);

        var result = await sut.ExtractAsync(Listing(parsed), CancellationToken.None);

        Assert.Equal(parsed, result.Value.ClosingDate);
    }

    [Fact]
    public async Task ExtractAsync_ServerError_ReturnsExtractionFailed()
    {
        var (sut, _, _) = Create(HttpStatusCode.InternalServerError, "{}");

        var result = await sut.ExtractAsync(Listing(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Extraction.Failed", result.Errors[0].Code);
    }

    [Fact]
    public async Task ExtractAsync_MalformedBody_ReturnsExtractionFailed()
    {
        var (sut, _, _) = Create(HttpStatusCode.OK, "not json");

        var result = await sut.ExtractAsync(Listing(), CancellationToken.None);

        Assert.Equal("Extraction.Failed", result.Errors[0].Code);
    }

    [Fact]
    public async Task ExtractAsync_AnyOutcome_NeverLogsApiKey()
    {
        foreach (var (status, body) in new[] { (HttpStatusCode.InternalServerError, "{}"), (HttpStatusCode.OK, "not json"), (HttpStatusCode.OK, ToolUseResponse) })
        {
            var (sut, _, logger) = Create(status, body);
            await sut.ExtractAsync(Listing(), CancellationToken.None);
            Assert.DoesNotContain(logger.Messages, m => m.Contains(ApiKey, StringComparison.Ordinal));
        }
    }
}
