using GetABursary.Core.Domain.Bursaries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GetABursary.Core.Data.Configurations.Bursaries;

public sealed class BursaryConfiguration : IEntityTypeConfiguration<Bursary>
{
    public void Configure(EntityTypeBuilder<Bursary> builder)
    {
        builder.ToTable("bursary");

        builder.Property(b => b.Slug).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(300).IsRequired();
        builder.Property(b => b.NormalizedName).HasMaxLength(300).IsRequired();
        builder.Property(b => b.Summary).HasMaxLength(600);
        builder.Property(b => b.ValueSummary).HasMaxLength(500);
        builder.Property(b => b.WorkBackDetails).HasMaxLength(1000);
        builder.Property(b => b.MinAverage).HasPrecision(5, 2);
        builder.Property(b => b.Citizenship).HasConversion<string>().IsRequired();
        builder.Property(b => b.Status).HasConversion<string>().IsRequired();
        builder.Property(b => b.ApplyUrl).HasMaxLength(1000);
        builder.Property(b => b.ExtractionConfidence).HasPrecision(3, 2);

        ConfigureEnumArray(builder.Property(b => b.Coverage));
        ConfigureEnumArray(builder.Property(b => b.StudyLevels));
        ConfigureEnumArray(builder.Property(b => b.Provinces));

        builder.Property(b => b.LockedFields)
            .HasColumnType("text[]")
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => a!.SequenceEqual(b!),
                l => l.Aggregate(0, (h, v) => HashCode.Combine(h, v.GetHashCode(StringComparison.Ordinal))),
                l => l.ToList()));

        builder.OwnsMany(b => b.MinSubjectRequirements, o => o.ToJson("min_subject_requirements"));

        builder.HasIndex(b => b.Slug).IsUnique();
        builder.HasIndex(b => b.NormalizedName)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(b => new { b.Status, b.ClosingDate });

        builder.HasOne(b => b.Funder)
            .WithMany()
            .HasForeignKey(b => b.FunderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(b => b.Fields).WithOne(x => x.Bursary).HasForeignKey(x => x.BursaryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(b => b.Qualifications).WithOne(x => x.Bursary).HasForeignKey(x => x.BursaryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(b => b.Institutions).WithOne(x => x.Bursary).HasForeignKey(x => x.BursaryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(b => b.Sources).WithOne(x => x.Bursary).HasForeignKey(x => x.BursaryId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureEnumArray<TEnum>(PropertyBuilder<List<TEnum>> property)
        where TEnum : struct, Enum
    {
        property
            .HasConversion(
                new ValueConverter<List<TEnum>, string[]>(
                    v => v.Select(e => e.ToString()).ToArray(),
                    v => v.Select(Enum.Parse<TEnum>).ToList()))
            .HasColumnType("text[]")
            .Metadata.SetValueComparer(new ValueComparer<List<TEnum>>(
                (a, b) => a!.SequenceEqual(b!),
                l => l.Aggregate(0, (h, v) => HashCode.Combine(h, v)),
                l => l.ToList()));
    }
}
