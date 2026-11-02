namespace GetABursary.Api.DTOs.Bursaries;

/// <summary>Full detail of an open bursary (FR-07): every summary field plus eligibility, value and the apply link.</summary>
public sealed record BursaryDetailResponse
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public string? FunderName { get; init; }

    public DateOnly? ClosingDate { get; init; }

    public required bool IsClosingSoon { get; init; }

    public required string Status { get; init; }

    public required IReadOnlyList<string> StudyLevels { get; init; }

    public decimal? MinAverage { get; init; }

    public required IReadOnlyList<string> Coverage { get; init; }

    public required IReadOnlyList<BursarySourceResponse> Sources { get; init; }

    public required decimal MatchScore { get; init; }

    public string? Summary { get; init; }

    public string? ValueSummary { get; init; }

    public bool? WorkBackRequired { get; init; }

    public string? WorkBackDetails { get; init; }

    public required IReadOnlyList<SubjectRequirementResponse> MinSubjectRequirements { get; init; }

    public required string Citizenship { get; init; }

    /// <summary>Empty means national.</summary>
    public required IReadOnlyList<string> Provinces { get; init; }

    public required IReadOnlyList<BursaryFieldResponse> Fields { get; init; }

    public required IReadOnlyList<BursaryQualificationResponse> Qualifications { get; init; }

    public required IReadOnlyList<BursaryInstitutionResponse> Institutions { get; init; }

    public DateOnly? OpenDate { get; init; }

    /// <summary>The funder's own page, or the most recently seen source listing when the funder's page is unknown.</summary>
    public string? ApplyUrl { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record SubjectRequirementResponse(string Subject, decimal MinPercent);

public sealed record BursaryFieldResponse(string Code, string Name);

public sealed record BursaryQualificationResponse(Guid Id, string Name, string InstitutionShortName);

public sealed record BursaryInstitutionResponse(Guid Id, string ShortName);
