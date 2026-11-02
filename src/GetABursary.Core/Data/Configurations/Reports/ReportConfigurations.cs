using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetABursary.Core.Data.Configurations.Reports;

public sealed class ListingReportConfiguration : IEntityTypeConfiguration<ListingReport>
{
    public void Configure(EntityTypeBuilder<ListingReport> builder)
    {
        builder.ToTable("listing_report");

        builder.Property(r => r.Reason).HasConversion<string>().IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.ContactEmail).HasMaxLength(254);
        builder.Property(r => r.IpHash).HasMaxLength(64).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().IsRequired();

        builder.HasOne(r => r.Bursary)
            .WithMany()
            .HasForeignKey(r => r.BursaryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ReviewItemConfiguration : IEntityTypeConfiguration<ReviewItem>
{
    public void Configure(EntityTypeBuilder<ReviewItem> builder)
    {
        builder.ToTable("review_item");

        builder.Property(r => r.Type).HasConversion<string>().IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().IsRequired();
        builder.Property(r => r.Details).HasColumnType("jsonb");
        builder.Property(r => r.Notes).HasMaxLength(2000).IsRequired();

        builder.HasIndex(r => new { r.Status, r.Type });

        builder.HasOne(r => r.Bursary)
            .WithMany()
            .HasForeignKey(r => r.BursaryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.RawListing)
            .WithMany()
            .HasForeignKey(r => r.RawListingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.ListingReport)
            .WithMany()
            .HasForeignKey(r => r.ListingReportId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
