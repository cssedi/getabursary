using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetABursary.Core.Data.Configurations.Ingestion;

public sealed class IngestionRunConfiguration : IEntityTypeConfiguration<IngestionRun>
{
    public void Configure(EntityTypeBuilder<IngestionRun> builder)
    {
        builder.ToTable("ingestion_run");

        builder.Property(r => r.Trigger).HasConversion<string>().IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().IsRequired();

        builder.HasMany(r => r.Sources)
            .WithOne(s => s.Run)
            .HasForeignKey(s => s.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class IngestionRunSourceConfiguration : IEntityTypeConfiguration<IngestionRunSource>
{
    public void Configure(EntityTypeBuilder<IngestionRunSource> builder)
    {
        builder.ToTable("ingestion_run_source");

        builder.Property(s => s.ErrorSummary).HasColumnType("text");

        builder.HasOne(s => s.Source)
            .WithMany()
            .HasForeignKey(s => s.SourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class IngestionRunRequestConfiguration : IEntityTypeConfiguration<IngestionRunRequest>
{
    public void Configure(EntityTypeBuilder<IngestionRunRequest> builder)
    {
        builder.ToTable("ingestion_run_request");

        builder.HasOne(r => r.Source)
            .WithMany()
            .HasForeignKey(r => r.SourceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
