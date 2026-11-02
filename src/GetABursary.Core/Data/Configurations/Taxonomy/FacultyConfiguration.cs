using GetABursary.Core.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetABursary.Core.Data.Configurations.Taxonomy;

public sealed class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> builder)
    {
        builder.ToTable("faculty");

        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Slug).HasMaxLength(120).IsRequired();

        builder.HasIndex(f => new { f.InstitutionId, f.Slug }).IsUnique();

        builder.HasMany(f => f.Qualifications)
            .WithOne(q => q.Faculty)
            .HasForeignKey(q => q.FacultyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
