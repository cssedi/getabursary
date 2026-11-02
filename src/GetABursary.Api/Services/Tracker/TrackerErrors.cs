using GetABursary.Core.Common;

namespace GetABursary.Api.Services.Tracker;

public static class TrackerErrors
{
    public static Error BursaryNotFound(Guid bursaryId) =>
        Error.NotFound("Bursary.NotFound", $"Bursary {bursaryId} was not found");

    public static Error StatusRequired() =>
        Error.Validation("Tracker.StatusRequired", "A status is required");
}
