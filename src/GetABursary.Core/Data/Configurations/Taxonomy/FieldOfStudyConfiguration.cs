using GetABursary.Core.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetABursary.Core.Data.Configurations.Taxonomy;

public sealed class FieldOfStudyConfiguration : IEntityTypeConfiguration<FieldOfStudy>
{
    public void Configure(EntityTypeBuilder<FieldOfStudy> builder)
    {
        builder.ToTable("field_of_study");

        builder.Property(f => f.Code).HasMaxLength(10).IsRequired();
        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Slug).HasMaxLength(120).IsRequired();

        builder.HasIndex(f => f.Code).IsUnique();
        builder.HasIndex(f => f.Slug).IsUnique();

        builder.HasOne(f => f.Parent)
            .WithMany(f => f.Children)
            .HasForeignKey(f => f.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
