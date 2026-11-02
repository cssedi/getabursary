using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Ingestion;

namespace GetABursary.Core.Repositories.Bursaries;

/// <summary>A possible duplicate of an incoming listing, found by trigram similarity of normalised names.</summary>
public sealed record BursaryCandidate(Guid Id, Guid? FunderId, DateOnly? ClosingDate, double Similarity);

public interface IBursaryWriteRepository
{
    /// <summary>Bursaries whose normalised name has trigram similarity at or above <paramref name="minSimilarity"/>, best first (read-only).</summary>
    Task<IReadOnlyList<BursaryCandidate>> FindCandidatesAsync(string normalizedName, double minSimilarity, CancellationToken ct);

    /// <summary>A tracked bursary with its field, qualification, institution and source links.</summary>
    Task<Bursary?> GetForMergeAsync(Guid id, CancellationToken ct);

    Task<bool> SlugExistsAsync(string slug, CancellationToken ct);

    Task<RawListing?> GetRawListingAsync(Guid rawListingId, CancellationToken ct);

    Task<IReadOnlyList<Guid>> GetFieldIdsByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken ct);

    /// <summary>Qualifications matched case-insensitively by name within the given institutions (matched by short name).</summary>
    Task<IReadOnlyList<Guid>> GetQualificationIdsAsync(
        IReadOnlyCollection<string> qualificationNames,
        IReadOnlyCollection<string> institutionShortNames,
        CancellationToken ct);

    Task<IReadOnlyList<Guid>> GetInstitutionIdsByShortNamesAsync(IReadOnlyCollection<string> shortNames, CancellationToken ct);

    /// <summary>Sets every published, not-yet-closed bursary whose closing date is before <paramref name="today"/> to Closed. Returns the count. Takes effect immediately.</summary>
    Task<int> CloseExpiredAsync(DateOnly today, DateTimeOffset now, CancellationToken ct);

    /// <summary>True when the bursary has a source link other than (<paramref name="sourceId"/>, <paramref name="url"/>) last seen at or after <paramref name="since"/>.</summary>
    Task<bool> HasOtherSourceSeenSinceAsync(Guid bursaryId, Guid sourceId, string url, DateTimeOffset since, CancellationToken ct);

    void Add(Bursary bursary);

    Task SaveChangesAsync(CancellationToken ct);
}
