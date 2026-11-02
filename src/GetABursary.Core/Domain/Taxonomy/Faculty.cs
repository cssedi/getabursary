using GetABursary.Core.Domain.Common;

namespace GetABursary.Core.Domain.Taxonomy;

public class Faculty : Entity
{
    public Guid InstitutionId { get; set; }

    public Institution Institution { get; set; } = null!;

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public ICollection<Qualification> Qualifications { get; set; } = [];
}
