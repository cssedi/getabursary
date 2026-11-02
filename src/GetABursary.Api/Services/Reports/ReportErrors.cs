using GetABursary.Core.Common;

namespace GetABursary.Api.Services.Reports;

public static class ReportErrors
{
    public static Error BursaryNotFound(Guid bursaryId) =>
        Error.NotFound("Bursary.NotFound", $"Bursary {bursaryId} was not found");

    public static Error ReasonRequired() =>
        Error.Validation("Report.ReasonRequired", "A reason is required");
}
