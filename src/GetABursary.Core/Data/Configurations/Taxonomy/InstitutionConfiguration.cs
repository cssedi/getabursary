using GetABursary.Core.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetABursary.Core.Data.Configurations.Taxonomy;

public sealed class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    public void Configure(EntityTypeBuilder<Institution> builder)
    {
        builder.ToTable("institution");

        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.ShortName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Slug).HasMaxLength(120).IsRequired();
        builder.Property(i => i.Type).HasConversion<string>().IsRequired();
        builder.Property(i => i.Province).HasConversion<string>().IsRequired();
        builder.Property(i => i.WebsiteUrl).IsRequired();

        builder.HasIndex(i => i.Slug).IsUnique();

        builder.HasMany(i => i.Faculties)
            .WithOne(f => f.Institution)
            .HasForeignKey(f => f.InstitutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
