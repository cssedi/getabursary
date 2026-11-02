namespace GetABursary.Core.Repositories.Bursaries;

/// <summary>Read-only bursary queries. Contains no business rules.</summary>
public interface IBursaryReadRepository
{
    /// <summary>Runs the eligibility filter, scoring, sorting and paging as one database query.</summary>
    Task<BursarySearchPage> SearchAsync(BursarySearchCriteria criteria, CancellationToken ct);
}
