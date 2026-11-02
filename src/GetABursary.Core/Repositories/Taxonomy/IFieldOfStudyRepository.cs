using GetABursary.Core.Domain.Taxonomy;

namespace GetABursary.Core.Repositories.Taxonomy;

public interface IFieldOfStudyRepository
{
    /// <summary>Every field of study, ordered by code (read-only).</summary>
    Task<IReadOnlyList<FieldOfStudy>> ListAllAsync(CancellationToken ct);
}
