using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common;

namespace GetABursary.Core.Domain.Ingestion;

/// <summary>Per-source counters for one ingestion run.</summary>
public class IngestionRunSource : Entity
{
    public Guid RunId { get; set; }

    public IngestionRun Run { get; set; } = null!;

    public Guid SourceId { get; set; }

    public Source Source { get; set; } = null!;

    public int Discovered { get; set; }

    public int Fetched { get; set; }

    public int Unchanged { get; set; }

    public int Created { get; set; }

    public int Updated { get; set; }

    public int Failed { get; set; }

    public string? ErrorSummary { get; set; }
}
