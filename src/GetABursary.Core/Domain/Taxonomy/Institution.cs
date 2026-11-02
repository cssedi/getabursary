using GetABursary.Core.Domain.Common;
using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Core.Domain.Taxonomy;

public class Institution : Entity
{
    public required string Name { get; set; }

    public required string ShortName { get; set; }

    public required string Slug { get; set; }

    public required InstitutionType Type { get; set; }

    public required Province Province { get; set; }

    public required string WebsiteUrl { get; set; }

    public ICollection<Faculty> Faculties { get; set; } = [];
}
