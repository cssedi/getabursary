namespace GetABursary.Api.DTOs.Taxonomy;

/// <summary>A CESM field of study. Top-level items are first-order fields; their second-order fields are in <see cref="Children"/>.</summary>
public sealed record FieldOfStudyResponse
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    /// <summary>Code of the first-order parent, or null for a first-order field.</summary>
    public string? ParentCode { get; init; }

    public required IReadOnlyList<FieldOfStudyResponse> Children { get; init; }
}
