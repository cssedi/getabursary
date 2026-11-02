namespace GetABursary.Core.Repositories.Taxonomy;

/// <summary>Looks up what a qualification maps to for bursary matching.</summary>
public interface IQualificationResolutionRepository
{
    /// <summary>The qualification's institution, its fields and their CESM parents; null when it does not exist.</summary>
    Task<QualificationResolution?> ResolveAsync(Guid qualificationId, CancellationToken ct);
}

/// <summary><c>FieldIds</c> contains the qualification's own fields plus their parent (first-order) fields, distinct.</summary>
public sealed record QualificationResolution(Guid QualificationId, Guid InstitutionId, IReadOnlyList<Guid> FieldIds);
