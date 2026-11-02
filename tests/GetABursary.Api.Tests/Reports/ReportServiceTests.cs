using System.Security.Cryptography;
using System.Text;
using GetABursary.Api.DTOs.Reports;
using GetABursary.Api.Services.Reports;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Common.Enums;
using Microsoft.Extensions.Options;

namespace GetABursary.Api.Tests.Reports;

public class ReportServiceTests
{
    private const string Salt = "unit-test-salt";
    private const string RemoteIp = "203.0.113.42";

    [Fact]
    public async Task CreateReportAsync_ValidRequest_StoresReportAndLinkedUserReviewItemInOneSave()
    {
        var bursaryId = Guid.CreateVersion7();
        var repository = new FakeListingReportRepository { KnownBursaryIds = [bursaryId] };
        var service = NewService(repository);

        var result = await service.CreateReportAsync(bursaryId, new CreateReportRequest
        {
            Reason = ReportReason.WrongClosingDate,
            Comment = "Closes 30 September.",
            ContactEmail = "student@example.test",
        }, RemoteIp, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.SaveChangesCalls);

        var report = Assert.Single(repository.Reports);
        Assert.Equal(result.Value.Id, report.Id);
        Assert.Equal(bursaryId, report.BursaryId);
        Assert.Equal(ReportReason.WrongClosingDate, report.Reason);
        Assert.Equal("Closes 30 September.", report.Comment);
        Assert.Equal("student@example.test", report.ContactEmail);
        Assert.Equal(ReviewStatus.Open, report.Status);

        var item = Assert.Single(repository.ReviewItems);
        Assert.Same(report, item.ListingReport);
        Assert.Equal(ReviewItemType.UserReport, item.Type);
        Assert.Equal(ReviewStatus.Open, item.Status);
        Assert.Equal(bursaryId, item.BursaryId);
    }

    [Fact]
    public async Task CreateReportAsync_UnknownBursary_ReturnsNotFoundAndStoresNothing()
    {
        var repository = new FakeListingReportRepository();
        var service = NewService(repository);
        var bursaryId = Guid.CreateVersion7();

        var result = await service.CreateReportAsync(bursaryId, new CreateReportRequest
        {
            Reason = ReportReason.BrokenLink,
        }, RemoteIp, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Errors[0].Type);
        Assert.Equal("Bursary.NotFound", result.Errors[0].Code);
        Assert.Empty(repository.Reports);
        Assert.Empty(repository.ReviewItems);
        Assert.Equal(0, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateReportAsync_MissingReason_ReturnsValidationErrorAndStoresNothing()
    {
        var bursaryId = Guid.CreateVersion7();
        var repository = new FakeListingReportRepository { KnownBursaryIds = [bursaryId] };
        var service = NewService(repository);

        var result = await service.CreateReportAsync(bursaryId, new CreateReportRequest(), RemoteIp, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Errors[0].Type);
        Assert.Empty(repository.Reports);
        Assert.Equal(0, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateReportAsync_RawIp_IsNeverStoredOnlyItsSaltedHash()
    {
        var bursaryId = Guid.CreateVersion7();
        var repository = new FakeListingReportRepository { KnownBursaryIds = [bursaryId] };
        var service = NewService(repository);

        await service.CreateReportAsync(bursaryId, new CreateReportRequest
        {
            Reason = ReportReason.Other,
            Comment = "Looks wrong.",
        }, RemoteIp, CancellationToken.None);

        var report = Assert.Single(repository.Reports);
        var expectedHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Salt + RemoteIp)));

        Assert.Equal(expectedHash, report.IpHash);
        Assert.Equal(64, report.IpHash.Length);
        Assert.DoesNotContain(RemoteIp, report.IpHash, StringComparison.Ordinal);
        Assert.DoesNotContain(RemoteIp, report.Comment, StringComparison.Ordinal);
        Assert.DoesNotContain(RemoteIp, report.ContactEmail ?? string.Empty, StringComparison.Ordinal);
    }

    private static ReportService NewService(FakeListingReportRepository repository) =>
        new(repository, Options.Create(new ReportsOptions { IpHashSalt = Salt }));
}
