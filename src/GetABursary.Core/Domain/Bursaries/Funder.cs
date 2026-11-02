using GetABursary.Core.Domain.Common;

namespace GetABursary.Core.Domain.Bursaries;

public class Funder : Entity
{
    public required string Name { get; set; }

    public required string NormalizedName { get; set; }

    public string? WebsiteUrl { get; set; }
}
