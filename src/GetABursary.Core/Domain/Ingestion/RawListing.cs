using System.Text.Json;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common;

namespace GetABursary.Core.Domain.Ingestion;

/// <summary>The latest snapshot of one listing page fetched from one source.</summary>
public class RawListing : Entity
{
    public Guid SourceId { get; set; }

    public Source Source { get; set; } = null!;

    public required string Url { get; set; }

    /// <summary>SHA-256 of the fetched HTML, lowercase hex.</summary>
    public required string ContentHash { get; set; }

    /// <summary>Latest fetched HTML only.</summary>
    public required string Html { get; set; }

    public JsonDocument? Parsed { get; set; }

    public JsonDocument? Extracted { get; set; }

    public Guid? BursaryId { get; set; }

    public Bursary? Bursary { get; set; }

    public DateTimeOffset FirstFetchedAt { get; set; }

    public DateTimeOffset LastFetchedAt { get; set; }

    public DateTimeOffset LastChangedAt { get; set; }

    public int MissedRuns { get; set; }
}
