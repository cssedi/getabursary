using System.Text.Json;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Reports;
using GetABursary.Core.Repositories.Bursaries;
using GetABursary.Core.Repositories.Ingestion;
using GetABursary.Core.Repositories.Reports;

namespace GetABursary.Worker.Pipeline.Jobs;

public interface IVanishedJob
{
    /// <summary>
    /// Counts a missed run for every listing of the source that was not seen this run, and raises a
    /// <see cref="ReviewItemType.Vanished"/> item for those that just reached two missed runs. Returns how many items were raised.
    /// </summary>
    Task<int> RunAsync(Guid sourceId, IReadOnlyCollection<Guid> seenRawListingIds, CancellationToken ct);
}

public sealed class VanishedJob(
    IRawListingRepository rawListings,
    IBursaryWriteRepository bursaries,
    IReviewItemRepository reviewItems,
    IClock clock) : IVanishedJob
{
    public const int MissedRunThreshold = 2;
    public static readonly TimeSpan OtherSourceWindow = TimeSpan.FromDays(7);

    public async Task<int> RunAsync(Guid sourceId, IReadOnlyCollection<Guid> seenRawListingIds, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(seenRawListingIds);

        var reached = await rawListings.IncrementMissedRunsAsync(sourceId, seenRawListingIds, MissedRunThreshold, ct);
        var since = clock.UtcNow - OtherSourceWindow;
        var raised = 0;

        foreach (var listing in reached)
        {
            if (await bursaries.HasOtherSourceSeenSinceAsync(listing.BursaryId, sourceId, listing.Url, since, ct)
                || await reviewItems.HasOpenAsync(listing.BursaryId, ReviewItemType.Vanished, ct))
            {
                continue;
            }

            reviewItems.Add(new ReviewItem
            {
                Type = ReviewItemType.Vanished,
                BursaryId = listing.BursaryId,
                RawListingId = listing.RawListingId,
                Details = JsonSerializer.SerializeToDocument(new { url = listing.Url, missedRuns = MissedRunThreshold }),
            });
            raised++;
        }

        if (raised > 0)
        {
            await rawListings.SaveChangesAsync(ct);
        }

        return raised;
    }
}
