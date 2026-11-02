using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common;

namespace GetABursary.Core.Domain.Users;

/// <summary>Where a student is with one bursary. "Saved" is simply the <see cref="Saved"/> status.</summary>
public enum TrackerStatus
{
    Saved,
    Preparing,
    Applied,
    Shortlisted,
    Awarded,
    Unsuccessful,
    Withdrawn,
}

/// <summary>One student's entry for one bursary (FR-12–FR-14). At most one per user and bursary.</summary>
public class TrackerEntry : Entity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid BursaryId { get; set; }

    public Bursary Bursary { get; set; } = null!;

    public TrackerStatus Status { get; set; }

    public DateOnly? AppliedOn { get; set; }

    public string? Notes { get; set; }
}
