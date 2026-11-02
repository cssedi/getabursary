using GetABursary.Core.Domain.Taxonomy;

namespace GetABursary.Core.Domain.Bursaries;

/// <summary>Join entity: a field of study a bursary is open to.</summary>
public class BursaryField
{
    public Guid BursaryId { get; set; }

    public Bursary Bursary { get; set; } = null!;

    public Guid FieldOfStudyId { get; set; }

    public FieldOfStudy FieldOfStudy { get; set; } = null!;
}

/// <summary>Join entity: a specific qualification named by the funder.</summary>
public class BursaryQualification
{
    public Guid BursaryId { get; set; }

    public Bursary Bursary { get; set; } = null!;

    public Guid QualificationId { get; set; }

    public Qualification Qualification { get; set; } = null!;
}

/// <summary>Join entity: an institution restriction. If any exist, only these institutions are eligible.</summary>
public class BursaryInstitution
{
    public Guid BursaryId { get; set; }

    public Bursary Bursary { get; set; } = null!;

    public Guid InstitutionId { get; set; }

    public Institution Institution { get; set; } = null!;
}

/// <summary>Records which aggregator page a bursary was found on.</summary>
public class BursarySource
{
    public Guid BursaryId { get; set; }

    public Bursary Bursary { get; set; } = null!;

    public Guid SourceId { get; set; }

    public Source Source { get; set; } = null!;

    public required string SourceUrl { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }
}
