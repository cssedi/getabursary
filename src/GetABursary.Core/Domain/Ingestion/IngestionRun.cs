using GetABursary.Core.Domain.Common;
using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Core.Domain.Ingestion;

/// <summary>One execution of the ingestion pipeline across one or more sources.</summary>
public class IngestionRun : Entity
{
    public IngestionTrigger Trigger { get; set; }

    public RunStatus Status { get; set; } = RunStatus.Running;

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    public ICollection<IngestionRunSource> Sources { get; set; } = [];
}
