using GetABursary.Core.Domain.Bursaries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetABursary.Core.Data.Configurations.Bursaries;

public sealed class BursaryFieldConfiguration : IEntityTypeConfiguration<BursaryField>
{
    public void Configure(EntityTypeBuilder<BursaryField> builder)
    {
        builder.ToTable("bursary_field");
        builder.HasKey(x => new { x.BursaryId, x.FieldOfStudyId });
        builder.HasOne(x => x.FieldOfStudy).WithMany().HasForeignKey(x => x.FieldOfStudyId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BursaryQualificationConfiguration : IEntityTypeConfiguration<BursaryQualification>
{
    public void Configure(EntityTypeBuilder<BursaryQualification> builder)
    {
        builder.ToTable("bursary_qualification");
        builder.HasKey(x => new { x.BursaryId, x.QualificationId });
        builder.HasOne(x => x.Qualification).WithMany().HasForeignKey(x => x.QualificationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BursaryInstitutionConfiguration : IEntityTypeConfiguration<BursaryInstitution>
{
    public void Configure(EntityTypeBuilder<BursaryInstitution> builder)
    {
        builder.ToTable("bursary_institution");
        builder.HasKey(x => new { x.BursaryId, x.InstitutionId });
        builder.HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BursarySourceConfiguration : IEntityTypeConfiguration<BursarySource>
{
    public void Configure(EntityTypeBuilder<BursarySource> builder)
    {
        builder.ToTable("bursary_source");
        builder.HasKey(x => new { x.BursaryId, x.SourceId, x.SourceUrl });
        builder.Property(x => x.SourceUrl).HasMaxLength(1000).IsRequired();
        builder.HasOne(x => x.Source).WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Cascade);
    }
}
