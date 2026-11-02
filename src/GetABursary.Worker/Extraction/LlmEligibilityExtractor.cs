using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Worker.Sources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GetABursary.Worker.Extraction;

/// <summary>Extracts eligibility with the Anthropic Messages API, forcing a single structured tool call. Only listing text is sent.</summary>
public sealed partial class LlmEligibilityExtractor(
    HttpClient http,
    IFieldCatalog fields,
    IOptions<ExtractionOptions> options,
    ILogger<LlmEligibilityExtractor> logger) : IEligibilityExtractor
{
    public const string ToolName = "record_eligibility";
    public static readonly Uri Endpoint = new("https://api.anthropic.com/v1/messages");

    private const decimal UnknownCodePenalty = 0.1m;

    public async Task<Result<ExtractedEligibility>> ExtractAsync(RawListing listing, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(listing);
        var opts = options.Value;
        try
        {
            var codes = await fields.GetCodesAsync(ct);
            var body = BuildRequestBody(listing, opts.Model, codes);

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(body),
            };
            request.Headers.Add("x-api-key", opts.ApiKey ?? string.Empty);
            request.Headers.Add("anthropic-version", "2023-06-01");

            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpFailure(logger, listing.SourceUrl, (int)response.StatusCode);
                return ExtractionErrors.Failed($"HTTP {(int)response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var input = FindToolInput(json);
            if (input is null)
            {
                LogNoToolCall(logger, listing.SourceUrl);
                return ExtractionErrors.Failed("no tool call in response");
            }

            return Map(input.Value, listing, codes);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or FormatException or InvalidOperationException or KeyNotFoundException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            // Log only the exception type: never the request, which carries the key header.
            LogFailure(logger, listing.SourceUrl, ex.GetType().Name);
            return ExtractionErrors.Failed(ex.GetType().Name);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Extraction request for {Url} failed with HTTP {Status}")]
    private static partial void LogHttpFailure(ILogger logger, Uri url, int status);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Extraction response for {Url} had no record_eligibility tool call")]
    private static partial void LogNoToolCall(ILogger logger, Uri url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Extraction for {Url} failed: {ExceptionType}")]
    private static partial void LogFailure(ILogger logger, Uri url, string exceptionType);

    private static JsonObject BuildRequestBody(RawListing listing, string model, IReadOnlyDictionary<string, string> codes) => new()
    {
        ["model"] = model,
        ["max_tokens"] = 1500,
        ["temperature"] = 0,
        ["system"] = BuildSystemPrompt(codes),
        ["tools"] = new JsonArray(new JsonObject
        {
            ["name"] = ToolName,
            ["description"] = "Record the structured eligibility found in the bursary listing.",
            ["input_schema"] = BuildSchema(),
        }),
        ["tool_choice"] = new JsonObject { ["type"] = "tool", ["name"] = ToolName },
        ["messages"] = new JsonArray(new JsonObject
        {
            ["role"] = "user",
            ["content"] = BuildUserText(listing),
        }),
    };

    private static string BuildSystemPrompt(IReadOnlyDictionary<string, string> codes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You extract structured eligibility criteria from South African bursary listings and record them with the record_eligibility tool.");
        sb.AppendLine("Rules:");
        sb.AppendLine("- Only use facts present in the text. If something is not stated, leave it empty, null or Unknown.");
        sb.AppendLine("- summary must be at most 600 characters, written in your own words. Do not copy sentences from the listing.");
        sb.AppendLine("- confidence is between 0 and 1 and reflects how explicit the text was.");
        sb.AppendLine("- fieldCodes must only use the CESM codes below. Set allFieldsEligible true when any field of study qualifies.");
        sb.AppendLine("- Dates are ISO yyyy-MM-dd. minAverage is a percentage from 0 to 100.");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- studyLevels: {EnumList<StudyLevel>()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- citizenship: {EnumList<Citizenship>()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- provinces: {EnumList<Province>()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- coverage: {EnumList<CoverageItem>()}");
        sb.AppendLine("Allowed CESM field codes:");
        foreach (var (code, name) in codes)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"{code} {name}");
        }

        return sb.ToString();
    }

    private static string EnumList<T>() where T : struct, Enum => string.Join(", ", Enum.GetNames<T>());

    private static string BuildUserText(RawListing l)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Title: {l.Title}");
        if (l.FunderName is not null) { sb.AppendLine(CultureInfo.InvariantCulture, $"Funder: {l.FunderName}"); }
        if (l.FieldsHint is not null) { sb.AppendLine(CultureInfo.InvariantCulture, $"Fields: {l.FieldsHint}"); }
        if (l.ValueText is not null) { sb.AppendLine(CultureInfo.InvariantCulture, $"Value: {l.ValueText}"); }
        if (l.ClosingDate is not null) { sb.AppendLine(CultureInfo.InvariantCulture, $"Closing date: {l.ClosingDate:yyyy-MM-dd}"); }
        sb.AppendLine("Eligibility text:");
        sb.AppendLine(l.EligibilityText);
        return sb.ToString();
    }

    private static JsonObject EnumArray<T>() where T : struct, Enum => new()
    {
        ["type"] = "array",
        ["items"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray(Enum.GetNames<T>().Select(n => (JsonNode)JsonValue.Create(n)!).ToArray()) },
    };

    private static JsonObject StringArray() => new() { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } };

    private static JsonObject BuildSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["fieldCodes"] = StringArray(),
            ["allFieldsEligible"] = new JsonObject { ["type"] = "boolean" },
            ["qualificationNames"] = StringArray(),
            ["institutionShortNames"] = StringArray(),
            ["studyLevels"] = EnumArray<StudyLevel>(),
            ["minAverage"] = new JsonObject { ["type"] = new JsonArray("number", "null") },
            ["subjectRequirements"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["subject"] = new JsonObject { ["type"] = "string" },
                        ["minPercent"] = new JsonObject { ["type"] = "number" },
                    },
                    ["required"] = new JsonArray("subject", "minPercent"),
                },
            },
            ["citizenship"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray(Enum.GetNames<Citizenship>().Select(n => (JsonNode)JsonValue.Create(n)!).ToArray()) },
            ["provinces"] = EnumArray<Province>(),
            ["coverage"] = EnumArray<CoverageItem>(),
            ["workBackRequired"] = new JsonObject { ["type"] = new JsonArray("boolean", "null") },
            ["workBackDetails"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
            ["closingDate"] = new JsonObject { ["type"] = new JsonArray("string", "null"), ["description"] = "yyyy-MM-dd" },
            ["openDate"] = new JsonObject { ["type"] = new JsonArray("string", "null"), ["description"] = "yyyy-MM-dd" },
            ["funderName"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
            ["summary"] = new JsonObject { ["type"] = "string", ["maxLength"] = 600 },
            ["confidence"] = new JsonObject { ["type"] = "number", ["minimum"] = 0, ["maximum"] = 1 },
        },
        ["required"] = new JsonArray("fieldCodes", "allFieldsEligible", "studyLevels", "citizenship", "summary", "confidence"),
    };

    private static JsonElement? FindToolInput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var block in content.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var t) && t.GetString() == "tool_use"
                && block.TryGetProperty("name", out var n) && n.GetString() == ToolName
                && block.TryGetProperty("input", out var input))
            {
                return input.Clone();
            }
        }

        return null;
    }

    private static Result<ExtractedEligibility> Map(JsonElement i, RawListing listing, IReadOnlyDictionary<string, string> allowed)
    {
        var confidence = Math.Clamp(Num(i, "confidence") ?? 0m, 0m, 1m);

        var fieldCodes = new List<string>();
        foreach (var code in Strings(i, "fieldCodes"))
        {
            if (allowed.ContainsKey(code))
            {
                if (!fieldCodes.Contains(code)) { fieldCodes.Add(code); }
            }
            else
            {
                confidence -= UnknownCodePenalty;
            }
        }

        confidence = Math.Max(confidence, 0m);

        var minAverage = Num(i, "minAverage");
        if (minAverage is not null)
        {
            minAverage = Math.Clamp(minAverage.Value, 0m, 100m);
        }

        var summary = Str(i, "summary") ?? string.Empty;
        if (summary.Length > 600)
        {
            summary = summary[..600];
        }

        var subjects = new List<SubjectRequirement>();
        if (i.TryGetProperty("subjectRequirements", out var sr) && sr.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in sr.EnumerateArray())
            {
                var subject = Str(s, "subject");
                var pct = Num(s, "minPercent");
                if (!string.IsNullOrWhiteSpace(subject) && pct is not null)
                {
                    subjects.Add(new SubjectRequirement(subject, Math.Clamp(pct.Value, 0m, 100m)));
                }
            }
        }

        var citizenship = Enum.TryParse<Citizenship>(Str(i, "citizenship"), out var c) ? c : Citizenship.Unknown;

        return new ExtractedEligibility(
            fieldCodes,
            Bool(i, "allFieldsEligible") ?? false,
            Strings(i, "qualificationNames"),
            Strings(i, "institutionShortNames"),
            Enums<StudyLevel>(i, "studyLevels"),
            minAverage,
            subjects,
            citizenship,
            Enums<Province>(i, "provinces"),
            Enums<CoverageItem>(i, "coverage"),
            Bool(i, "workBackRequired"),
            Str(i, "workBackDetails"),
            listing.ClosingDate ?? Date(i, "closingDate"),
            listing.OpenDate ?? Date(i, "openDate"),
            Str(i, "funderName") ?? listing.FunderName,
            summary,
            confidence);
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static bool? Bool(JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind is JsonValueKind.True or JsonValueKind.False ? p.GetBoolean() : null;

    private static decimal? Num(JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : null;

    private static DateOnly? Date(JsonElement e, string name) =>
        DateOnly.TryParseExact(Str(e, name), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    private static List<string> Strings(JsonElement e, string name)
    {
        var list = new List<string>();
        if (e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Array)
        {
            foreach (var x in p.EnumerateArray())
            {
                if (x.ValueKind == JsonValueKind.String && x.GetString() is { Length: > 0 } s) { list.Add(s); }
            }
        }

        return list;
    }

    private static List<T> Enums<T>(JsonElement e, string name) where T : struct, Enum =>
        Strings(e, name).Select(s => Enum.TryParse<T>(s, out var v) ? (T?)v : null)
            .Where(v => v is not null).Select(v => v!.Value).Distinct().ToList();
}
