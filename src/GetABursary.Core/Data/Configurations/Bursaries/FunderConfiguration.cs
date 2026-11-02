using GetABursary.Core.Domain.Bursaries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetABursary.Core.Data.Configurations.Bursaries;

public sealed class FunderConfiguration : IEntityTypeConfiguration<Funder>
{
    public void Configure(EntityTypeBuilder<Funder> builder)
    {
        builder.ToTable("funder");

        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.NormalizedName).HasMaxLength(200).IsRequired();
        builder.Property(f => f.WebsiteUrl).HasMaxLength(500);

        builder.HasIndex(f => f.NormalizedName).IsUnique();
    }
}
