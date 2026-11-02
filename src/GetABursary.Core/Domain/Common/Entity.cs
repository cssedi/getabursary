namespace GetABursary.Core.Domain.Common;

/// <summary>Base class for all persisted entities: a version 7 GUID key plus audit timestamps.</summary>
public abstract class Entity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
