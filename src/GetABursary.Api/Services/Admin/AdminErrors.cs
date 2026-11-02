using GetABursary.Core.Common;

namespace GetABursary.Api.Services.Admin;

public static class AdminErrors
{
    public static Error ReviewItemNotFound(Guid id) =>
        Error.NotFound("ReviewItem.NotFound", $"Review item {id} was not found");

    public static Error ReviewItemNotOpen() =>
        Error.Conflict("ReviewItem.NotOpen", "The review item has already been resolved");

    public static Error NoBursary() =>
        Error.Validation("ReviewItem.NoBursary", "The review item has no bursary to publish or close");

    public static Error InvalidAction() =>
        Error.Validation("ReviewItem.InvalidAction", "action must be Publish, Dismiss or MarkClosed");

    public static Error InvalidNotes() =>
        Error.Validation("ReviewItem.InvalidNotes", "notes must be 2000 characters or fewer");

    public static Error InvalidPage() =>
        Error.Validation("Admin.InvalidPage", "page must be 1 or greater");

    public static Error BursaryNotFound(Guid id) =>
        Error.NotFound("Bursary.NotFound", $"Bursary {id} was not found");

    public static Error LockNotFound(string field) =>
        Error.NotFound("Bursary.LockNotFound", $"Field '{field}' is not locked");

    public static Error InvalidField(string field, string rule) =>
        Error.Validation("Bursary.InvalidField", $"{field}: {rule}");

    public static Error UnknownField(string field) =>
        Error.Validation("Bursary.UnknownField", $"'{field}' is not an editable field");
}
