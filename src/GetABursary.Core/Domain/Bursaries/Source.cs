using GetABursary.Core.Domain.Common;

namespace GetABursary.Core.Domain.Bursaries;

/// <summary>An aggregator website that bursaries are discovered on.</summary>
public class Source : Entity
{
    public required string Key { get; set; }

    public required string Name { get; set; }

    public required string BaseUrl { get; set; }

    public string? IconUrl { get; set; }

    public bool Enabled { get; set; }

    public int CrawlDelayMs { get; set; } = 3000;
}
