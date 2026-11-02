using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Api.DTOs.Taxonomy;

public sealed record QualificationResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Abbreviation { get; init; }

    public required StudyLevel StudyLevel { get; init; }

    public required string Slug { get; init; }

    /// <summary>CESM codes of the fields this qualification covers, sorted.</summary>
    public required IReadOnlyList<string> FieldCodes { get; init; }
}
