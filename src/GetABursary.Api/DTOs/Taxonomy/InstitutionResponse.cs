using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Api.DTOs.Taxonomy;

public sealed record InstitutionResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string ShortName { get; init; }

    public required string Slug { get; init; }

    public required InstitutionType Type { get; init; }

    public required Province Province { get; init; }
}
