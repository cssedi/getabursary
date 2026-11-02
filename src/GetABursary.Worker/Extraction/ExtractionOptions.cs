namespace GetABursary.Worker.Extraction;

/// <summary>Bound from the <c>Extraction</c> section. <c>ApiKey</c> must come from user-secrets or environment, never from a committed file.</summary>
public sealed class ExtractionOptions
{
    public const string SectionName = "Extraction";

    public string Provider { get; set; } = "fake";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-haiku-5-5";
}
