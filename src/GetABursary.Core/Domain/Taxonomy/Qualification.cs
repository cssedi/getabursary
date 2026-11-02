using GetABursary.Core.Domain.Common;
using GetABursary.Core.Domain.Common.Enums;

namespace GetABursary.Core.Domain.Taxonomy;

public class Qualification : Entity
{
    public Guid FacultyId { get; set; }

    public Faculty Faculty { get; set; } = null!;

    public required string Name { get; set; }

    public required string Abbreviation { get; set; }

    public required StudyLevel StudyLevel { get; set; }

    public int? NqfLevel { get; set; }

    public int? DurationYears { get; set; }

    public required string Slug { get; set; }

    public ICollection<QualificationField> QualificationFields { get; set; } = [];
}
