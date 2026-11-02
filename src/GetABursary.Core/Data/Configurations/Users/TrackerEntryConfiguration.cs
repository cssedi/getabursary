using GetABursary.Core.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetABursary.Core.Data.Configurations.Users;

public sealed class TrackerEntryConfiguration : IEntityTypeConfiguration<TrackerEntry>
{
    public void Configure(EntityTypeBuilder<TrackerEntry> builder)
    {
        builder.ToTable("tracker_entry");

        builder.Property(t => t.Status).HasConversion<string>().IsRequired();
        builder.Property(t => t.Notes).HasMaxLength(2000);

        builder.HasIndex(t => new { t.UserId, t.BursaryId }).IsUnique();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Bursary)
            .WithMany()
            .HasForeignKey(t => t.BursaryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
