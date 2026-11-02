using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Repositories.Bursaries;

namespace GetABursary.Api.Tests.Bursaries.Detail;

/// <summary>In-memory stand-in for <see cref="IBursaryReadRepository"/>, keyed by slug.</summary>
public sealed class FakeBursaryReadRepository : IBursaryReadRepository
{
    public Dictionary<string, Bursary> BySlug { get; } = [];

    public Task<Bursary?> GetBySlugAsync(string slug, CancellationToken ct) =>
        Task.FromResult(BySlug.GetValueOrDefault(slug));

    public Task<BursarySearchPage> SearchAsync(BursarySearchCriteria criteria, CancellationToken ct) =>
        throw new NotSupportedException("The detail service never searches.");
}
