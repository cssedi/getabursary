using GetABursary.Core.Domain.Bursaries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetABursary.Core.Data.Configurations.Bursaries;

public sealed class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        builder.ToTable("source");

        builder.Property(s => s.Key).HasMaxLength(60).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.BaseUrl).HasMaxLength(500).IsRequired();
        builder.Property(s => s.IconUrl).HasMaxLength(500);
        builder.Property(s => s.CrawlDelayMs).HasDefaultValue(3000);

        builder.HasIndex(s => s.Key).IsUnique();
    }
}
