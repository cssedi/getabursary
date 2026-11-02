using GetABursary.Core.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetABursary.Core.Data.Configurations.Taxonomy;

public sealed class QualificationFieldConfiguration : IEntityTypeConfiguration<QualificationField>
{
    public void Configure(EntityTypeBuilder<QualificationField> builder)
    {
        builder.ToTable("qualification_field");

        builder.HasKey(qf => new { qf.QualificationId, qf.FieldOfStudyId });

        builder.HasOne(qf => qf.FieldOfStudy)
            .WithMany()
            .HasForeignKey(qf => qf.FieldOfStudyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
