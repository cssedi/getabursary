using GetABursary.Api.DTOs.Bursaries;
using GetABursary.Core.Common;

namespace GetABursary.Api.Services.Bursaries;

public static class BursarySearchErrors
{
    public const int MaxPageSize = 50;

    public static Error InvalidAverage() =>
        Error.Validation("Search.InvalidAverage", "average must be between 0 and 100");

    public static Error InvalidPage() =>
        Error.Validation("Search.InvalidPage", "page must be 1 or greater");

    public static Error InvalidPageSize() =>
        Error.Validation("Search.InvalidPageSize", $"pageSize must be between 1 and {MaxPageSize}");

    public static Error InvalidDateRange() =>
        Error.Validation("Search.InvalidDateRange", "closingFrom must not be after closingTo");

    public static Error UnknownQualification(Guid qualificationId) =>
        Error.Validation("Search.UnknownQualification", $"Qualification {qualificationId} does not exist");
}

/// <summary>Request-shape rules for the search endpoint (everything checkable without the database).</summary>
public static class BursarySearchValidator
{
    public static IReadOnlyList<Error> Validate(BursarySearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<Error>();

        if (request.Average is < 0 or > 100)
        {
            errors.Add(BursarySearchErrors.InvalidAverage());
        }

        if (request.Page < 1)
        {
            errors.Add(BursarySearchErrors.InvalidPage());
        }

        if (request.PageSize is < 1 or > BursarySearchErrors.MaxPageSize)
        {
            errors.Add(BursarySearchErrors.InvalidPageSize());
        }

        if (request is { ClosingFrom: { } from, ClosingTo: { } to } && from > to)
        {
            errors.Add(BursarySearchErrors.InvalidDateRange());
        }

        return errors;
    }
}
