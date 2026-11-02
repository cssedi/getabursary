using GetABursary.Core.Domain.Bursaries;

namespace GetABursary.Core.Repositories.Bursaries;

/// <summary>Read-only bursary queries. Contains no business rules.</summary>
public interface IBursaryReadRepository
{
    /// <summary>Runs the eligibility filter, scoring, sorting and paging as one database query.</summary>
    Task<BursarySearchPage> SearchAsync(BursarySearchCriteria criteria, CancellationToken ct);

    /// <summary>The bursary with its detail graph loaded, published or not; null when the slug is unknown.</summary>
    Task<Bursary?> GetBySlugAsync(string slug, CancellationToken ct);
}
