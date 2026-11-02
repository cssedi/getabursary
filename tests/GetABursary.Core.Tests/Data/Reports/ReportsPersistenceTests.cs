using System.Text.Json;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Reports;
using GetABursary.Core.Tests.Data.Taxonomy;
using GetABursary.Core.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Tests.Data.Reports;

[Collection("Postgres")]
public class ReportsPersistenceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task ListingReport_Saves_And_Reloads_With_Every_Value_Intact()
    {
        var bursary = NewBursary();
        var report = new ListingReport
        {
            Bursary = bursary,
            Reason = ReportReason.WrongClosingDate,
            Comment = "The closing date is 30 September, not 30 August.",
            ContactEmail = "student@example.test",
            IpHash = new string('d', 64),
            Status = ReviewStatus.Open,
        };

        await using (var writer = fixture.CreateContext())
        {
            writer.ListingReports.Add(report);
            await writer.SaveChangesAsync();
        }

        await using var reader = fixture.CreateContext();
        var loaded = await reader.ListingReports.AsNoTracking().SingleAsync(r => r.Id == report.Id);

        Assert.Equal(bursary.Id, loaded.BursaryId);
        Assert.Equal(ReportReason.WrongClosingDate, loaded.Reason);
        Assert.Equal("The closing date is 30 September, not 30 August.", loaded.Comment);
        Assert.Equal("student@example.test", loaded.ContactEmail);
        Assert.Equal(new string('d', 64), loaded.IpHash);
        Assert.Equal(ReviewStatus.Open, loaded.Status);
    }

    [Fact]
    public async Task ListingReport_Comment_Over_1000_Chars_Throws()
    {
        var bursary = NewBursary();
        await using var context = fixture.CreateContext();
        context.ListingReports.Add(new ListingReport
        {
            Bursary = bursary,
            Reason = ReportReason.Other,
            Comment = new string('x', 1001),
            IpHash = new string('e', 64),
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task ReviewItem_Saves_And_Reloads_With_Jsonb_Details_And_Links()
    {
        var bursary = NewBursary();
        var report = new ListingReport
        {
            Bursary = bursary,
            Reason = ReportReason.BrokenLink,
            Comment = "404 on apply",
            IpHash = new string('f', 64),
        };
        var resolvedBy = Guid.CreateVersion7();
        var resolvedAt = new DateTimeOffset(2026, 5, 4, 12, 0, 0, TimeSpan.Zero);
        var item = new ReviewItem
        {
            Type = ReviewItemType.UserReport,
            Status = ReviewStatus.Resolved,
            Bursary = bursary,
            ListingReport = report,
            Details = JsonDocument.Parse("""{"oldClosing":"2026-08-30","newClosing":"2026-09-30"}"""),
            ResolvedByUserId = resolvedBy,
            ResolvedAt = resolvedAt,
            Notes = "Confirmed on the funder's site.",
        };

        await using (var writer = fixture.CreateContext())
        {
            writer.ReviewItems.Add(item);
            await writer.SaveChangesAsync();
        }

        await using var reader = fixture.CreateContext();
        var loaded = await reader.ReviewItems.AsNoTracking().SingleAsync(r => r.Id == item.Id);

        Assert.Equal(ReviewItemType.UserReport, loaded.Type);
        Assert.Equal(ReviewStatus.Resolved, loaded.Status);
        Assert.Equal(bursary.Id, loaded.BursaryId);
        Assert.Equal(report.Id, loaded.ListingReportId);
        Assert.Null(loaded.RawListingId);
        Assert.NotNull(loaded.Details);
        Assert.Equal("2026-09-30", loaded.Details!.RootElement.GetProperty("newClosing").GetString());
        Assert.Equal(resolvedBy, loaded.ResolvedByUserId);
        Assert.Equal(resolvedAt, loaded.ResolvedAt);
        Assert.Equal("Confirmed on the funder's site.", loaded.Notes);
    }

    [Fact]
    public async Task ReviewItem_Null_Details_Reloads_As_Null()
    {
        var item = new ReviewItem { Type = ReviewItemType.Vanished };

        await using (var writer = fixture.CreateContext())
        {
            writer.ReviewItems.Add(item);
            await writer.SaveChangesAsync();
        }

        await using var reader = fixture.CreateContext();
        var loaded = await reader.ReviewItems.AsNoTracking().SingleAsync(r => r.Id == item.Id);

        Assert.Equal(ReviewStatus.Open, loaded.Status);
        Assert.Null(loaded.Details);
        Assert.Equal(string.Empty, loaded.Notes);
    }

    [Fact]
    public async Task ReviewItem_Notes_Over_2000_Chars_Throws()
    {
        await using var context = fixture.CreateContext();
        context.ReviewItems.Add(new ReviewItem
        {
            Type = ReviewItemType.NewListing,
            Notes = new string('n', 2001),
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static Bursary NewBursary() => new()
    {
        Slug = TaxonomyPersistenceTests.UniqueSlug(),
        Name = "Reported Bursary",
        NormalizedName = "reported bursary",
    };
}
