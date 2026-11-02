using GetABursary.Core.Domain.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetABursary.Core.Data.Configurations.Ingestion;

public sealed class RawListingConfiguration : IEntityTypeConfiguration<RawListing>
{
    public void Configure(EntityTypeBuilder<RawListing> builder)
    {
        builder.ToTable("raw_listing");

        builder.Property(r => r.Url).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(r => r.Html).HasColumnType("text").IsRequired();
        builder.Property(r => r.Parsed).HasColumnType("jsonb");
        builder.Property(r => r.Extracted).HasColumnType("jsonb");

        builder.HasIndex(r => new { r.SourceId, r.Url }).IsUnique();

        builder.HasOne(r => r.Source)
            .WithMany()
            .HasForeignKey(r => r.SourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Bursary)
            .WithMany()
            .HasForeignKey(r => r.BursaryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
