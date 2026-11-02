using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common;

namespace GetABursary.Core.Domain.Ingestion;

/// <summary>An admin's request for a manual ingestion run, picked up by the worker.</summary>
public class IngestionRunRequest : Entity
{
    /// <summary>Null means all enabled sources.</summary>
    public Guid? SourceId { get; set; }

    public Source? Source { get; set; }

    /// <summary>Identity user id. No foreign key: the Identity tables arrive in a later task.</summary>
    public Guid RequestedByUserId { get; set; }

    public DateTimeOffset RequestedAt { get; set; }

    public DateTimeOffset? PickedUpAt { get; set; }
}
