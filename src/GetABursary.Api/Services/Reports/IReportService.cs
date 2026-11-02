using GetABursary.Api.DTOs.Reports;
using GetABursary.Core.Common;

namespace GetABursary.Api.Services.Reports;

public interface IReportService
{
    /// <summary>Records an anonymous inaccuracy report and its review item. <paramref name="remoteIp"/> is hashed, never stored.</summary>
    Task<Result<CreateReportResponse>> CreateReportAsync(
        Guid bursaryId,
        CreateReportRequest request,
        string remoteIp,
        CancellationToken ct);
}
