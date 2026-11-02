using GetABursary.Core.Domain.Ingestion;

namespace GetABursary.Core.Repositories.Ingestion;

public interface IRawListingRepository
{
    /// <summary>The tracked snapshot for (source, url), or <c>null</c>.</summary>
    Task<RawListing?> GetBySourceAndUrlAsync(Guid sourceId, string url, CancellationToken ct);

    /// <summary>Adds a new snapshot. Call <see cref="SaveChangesAsync"/> to persist it.</summary>
    void Add(RawListing listing);

    Task SaveChangesAsync(CancellationToken ct);
}
