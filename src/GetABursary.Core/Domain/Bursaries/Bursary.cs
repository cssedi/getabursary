using GetABursary.Core.Domain.Common;
using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Core.Domain.Bursaries;

public class Bursary : Entity
{
    private const int ClosingSoonDays = 14;

    public required string Slug { get; set; }

    public required string Name { get; set; }

    public required string NormalizedName { get; set; }

    public Guid? FunderId { get; set; }

    public Funder? Funder { get; set; }

    public string? Summary { get; set; }

    public string? ValueSummary { get; set; }

    public List<CoverageItem> Coverage { get; set; } = [];

    public bool? WorkBackRequired { get; set; }

    public string? WorkBackDetails { get; set; }

    /// <summary>Empty means unknown.</summary>
    public List<StudyLevel> StudyLevels { get; set; } = [];

    public decimal? MinAverage { get; set; }

    public List<SubjectRequirement> MinSubjectRequirements { get; set; } = [];

    public Citizenship Citizenship { get; set; } = Citizenship.Unknown;

    /// <summary>Empty means national.</summary>
    public List<Province> Provinces { get; set; } = [];

    public bool AllFieldsEligible { get; set; }

    public DateOnly? OpenDate { get; set; }

    public DateOnly? ClosingDate { get; set; }

    public BursaryStatus Status { get; set; } = BursaryStatus.Unknown;

    public string? ApplyUrl { get; set; }

    public decimal ExtractionConfidence { get; set; }

    public List<string> LockedFields { get; set; } = [];

    public bool IsPublished { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    public ICollection<BursaryField> Fields { get; set; } = [];

    public ICollection<BursaryQualification> Qualifications { get; set; } = [];

    public ICollection<BursaryInstitution> Institutions { get; set; } = [];

    public ICollection<BursarySource> Sources { get; set; } = [];

    /// <summary>True when the bursary is open and closes between today and 14 days from today (inclusive).</summary>
    public bool IsClosingSoon(DateOnly today) =>
        Status == BursaryStatus.Open
        && ClosingDate is { } closing
        && closing >= today
        && closing <= today.AddDays(ClosingSoonDays);

    /// <summary>True when the named field is locked against ingestion overwrites.</summary>
    public bool IsLocked(string fieldName) =>
        LockedFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
}
