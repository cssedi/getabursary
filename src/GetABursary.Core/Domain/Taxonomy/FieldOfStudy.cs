using GetABursary.Core.Domain.Common;

namespace GetABursary.Core.Domain.Taxonomy;

/// <summary>A CESM field of study. First-order fields have no parent; second-order fields point at their first-order parent.</summary>
public class FieldOfStudy : Entity
{
    public required string Code { get; set; }

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public Guid? ParentId { get; set; }

    public FieldOfStudy? Parent { get; set; }

    public ICollection<FieldOfStudy> Children { get; set; } = [];
}
