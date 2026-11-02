namespace GetABursary.Api.DTOs.Taxonomy;

/// <summary>A faculty with the qualifications it offers, for one institution's degree picker.</summary>
public sealed record FacultyQualificationsResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public required IReadOnlyList<QualificationResponse> Qualifications { get; init; }
}
