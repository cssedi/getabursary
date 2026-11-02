using GetABursary.Api.DTOs.Bursaries;
using GetABursary.Api.DTOs.Tracker;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Users;
using GetABursary.Core.Repositories.Tracker;

namespace GetABursary.Api.Services.Tracker;

public sealed class TrackerService(ITrackerRepository tracker, IClock clock) : ITrackerService
{
    public async Task<Result<IReadOnlyList<TrackerEntryResponse>>> ListAsync(Guid userId, CancellationToken ct)
    {
        var entries = await tracker.ListAsync(userId, ct);
        var today = clock.Today;

        IReadOnlyList<TrackerEntryResponse> items = [.. entries.Select(e => ToResponse(e, e.Bursary, today))];
        return Result<IReadOnlyList<TrackerEntryResponse>>.Success(items);
    }

    public async Task<Result<UpsertTrackerOutcome>> UpsertAsync(Guid userId, Guid bursaryId, UpsertTrackerRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Status is not { } status)
        {
            return TrackerErrors.StatusRequired();
        }

        var bursary = await tracker.GetPublishedBursaryAsync(bursaryId, ct);
        if (bursary is null)
        {
            return TrackerErrors.BursaryNotFound(bursaryId);
        }

        var entry = await tracker.FindAsync(userId, bursaryId, ct);
        var created = entry is null;
        if (entry is null)
        {
            entry = new TrackerEntry { UserId = userId, BursaryId = bursaryId };
            tracker.Add(entry);
        }

        entry.Status = status;
        entry.AppliedOn = request.AppliedOn;
        entry.Notes = request.Notes;

        await tracker.SaveChangesAsync(ct);

        return new UpsertTrackerOutcome(created, ToResponse(entry, bursary, clock.Today));
    }

    public Task DeleteAsync(Guid userId, Guid bursaryId, CancellationToken ct) =>
        tracker.DeleteAsync(userId, bursaryId, ct);

    private static TrackerEntryResponse ToResponse(TrackerEntry entry, Bursary bursary, DateOnly today) => new()
    {
        Bursary = ToSummary(bursary, today),
        Status = entry.Status,
        AppliedOn = entry.AppliedOn,
        Notes = entry.Notes,
        UpdatedAt = entry.UpdatedAt,
    };

    // shortcut: duplicates BursaryService.ToSummary (outside T-041 scope); move both to one shared mapper when a third caller appears.
    private static BursarySummaryResponse ToSummary(Bursary bursary, DateOnly today) => new()
    {
        Id = bursary.Id,
        Slug = bursary.Slug,
        Name = bursary.Name,
        FunderName = bursary.Funder?.Name,
        ClosingDate = bursary.ClosingDate,
        IsClosingSoon = bursary.IsClosingSoon(today),
        Status = bursary.Status.ToString(),
        StudyLevels = [.. bursary.StudyLevels.Select(l => l.ToString())],
        MinAverage = bursary.MinAverage,
        Coverage = [.. bursary.Coverage.Select(c => c.ToString())],
        Sources = [.. bursary.Sources
            .OrderBy(s => s.Source.Name)
            .ThenBy(s => s.SourceUrl, StringComparer.Ordinal)
            .Select(s => new BursarySourceResponse
            {
                Key = s.Source.Key,
                Name = s.Source.Name,
                IconUrl = s.Source.IconUrl,
                Url = s.SourceUrl,
            })],
        MatchScore = 0m,
    };
}
