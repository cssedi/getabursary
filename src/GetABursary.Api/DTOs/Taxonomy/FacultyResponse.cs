namespace GetABursary.Api.DTOs.Taxonomy;

public sealed record FacultyResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }
}
