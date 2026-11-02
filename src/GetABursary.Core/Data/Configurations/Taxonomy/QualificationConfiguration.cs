using GetABursary.Core.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetABursary.Core.Data.Configurations.Taxonomy;

public sealed class QualificationConfiguration : IEntityTypeConfiguration<Qualification>
{
    public void Configure(EntityTypeBuilder<Qualification> builder)
    {
        builder.ToTable("qualification");

        builder.Property(q => q.Name).HasMaxLength(200).IsRequired();
        builder.Property(q => q.Abbreviation).HasMaxLength(200).IsRequired();
        builder.Property(q => q.StudyLevel).HasConversion<string>().IsRequired();
        builder.Property(q => q.Slug).HasMaxLength(120).IsRequired();

        builder.HasIndex(q => new { q.FacultyId, q.Slug }).IsUnique();

        builder.HasMany(q => q.QualificationFields)
            .WithOne(qf => qf.Qualification)
            .HasForeignKey(qf => qf.QualificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
