using GetABursary.Worker.Extraction;
using GetABursary.Worker.Sources;

namespace GetABursary.Worker.Pipeline.Merging;

public enum MergeKind
{
    Created,
    Merged,
    PossibleDuplicate,
}

/// <summary>The result of merging one listing. For <see cref="MergeKind.PossibleDuplicate"/> the bursary is a new, unpublished one.</summary>
public sealed record MergeOutcome(MergeKind Kind, Guid BursaryId);

public interface IBursaryMerger
{
    Task<MergeOutcome> MergeAsync(
        Guid sourceId,
        Uri sourceUrl,
        Guid rawListingId,
        RawListing raw,
        ExtractedEligibility ex,
        CancellationToken ct);
}
