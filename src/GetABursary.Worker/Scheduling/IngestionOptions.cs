namespace GetABursary.Worker.Scheduling;

/// <summary>Bound from the <c>Ingestion</c> configuration section.</summary>
public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    public string Cron { get; set; } = "0 3 * * *";

    public string TimeZone { get; set; } = "Africa/Johannesburg";

    public int RequestPollSeconds { get; set; } = 60;
}
