using System.Security.Cryptography;
using System.Text;
using GetABursary.Api.DTOs.Reports;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Reports;
using GetABursary.Core.Repositories.Reports;
using Microsoft.Extensions.Options;

namespace GetABursary.Api.Services.Reports;

public sealed class ReportService(
    IListingReportRepository repository,
    IOptions<ReportsOptions> options) : IReportService
{
    public async Task<Result<CreateReportResponse>> CreateReportAsync(
        Guid bursaryId,
        CreateReportRequest request,
        string remoteIp,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(remoteIp);

        if (request.Reason is not { } reason)
        {
            return ReportErrors.ReasonRequired();
        }

        if (!await repository.BursaryExistsAsync(bursaryId, ct))
        {
            return ReportErrors.BursaryNotFound(bursaryId);
        }

        var report = new ListingReport
        {
            BursaryId = bursaryId,
            Reason = reason,
            Comment = request.Comment ?? string.Empty,
            ContactEmail = request.ContactEmail,
            IpHash = HashIp(remoteIp),
            Status = ReviewStatus.Open,
        };

        repository.AddReport(report);
        repository.AddReviewItem(new ReviewItem
        {
            Type = ReviewItemType.UserReport,
            Status = ReviewStatus.Open,
            BursaryId = bursaryId,
            ListingReport = report,
            Notes = string.Empty,
        });

        await repository.SaveChangesAsync(ct);

        return new CreateReportResponse { Id = report.Id };
    }

    /// <summary>Lower-case hex SHA-256 of <c>IpHashSalt + remoteIp</c> (specs/03-conventions.md §7).</summary>
    private string HashIp(string remoteIp)
    {
        var bytes = Encoding.UTF8.GetBytes(options.Value.IpHashSalt + remoteIp);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
