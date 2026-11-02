namespace GetABursary.Core.Domain.Taxonomy;

/// <summary>
/// Join entity linking a qualification to the fields of study it covers.
/// Keyed by the pair of foreign keys; deliberately does not inherit <c>Entity</c> (no id or audit columns).
/// </summary>
public class QualificationField
{
    public Guid QualificationId { get; set; }

    public Qualification Qualification { get; set; } = null!;

    public Guid FieldOfStudyId { get; set; }

    public FieldOfStudy FieldOfStudy { get; set; } = null!;
}
