using GetABursary.Core.Domain.Bursaries;

namespace GetABursary.Core.Repositories.Bursaries;

public interface IFunderRepository
{
    Task<Funder?> GetByNormalizedNameAsync(string normalizedName, CancellationToken ct);

    void Add(Funder funder);
}
