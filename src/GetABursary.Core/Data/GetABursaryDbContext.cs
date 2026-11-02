using GetABursary.Core.Common;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common;
using GetABursary.Core.Domain.Ingestion;
using GetABursary.Core.Domain.Reports;
using GetABursary.Core.Domain.Taxonomy;
using GetABursary.Core.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Data;

public class GetABursaryDbContext(DbContextOptions<GetABursaryDbContext> options, IClock clock)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<FieldOfStudy> FieldsOfStudy => Set<FieldOfStudy>();

    public DbSet<Institution> Institutions => Set<Institution>();

    public DbSet<Faculty> Faculties => Set<Faculty>();

    public DbSet<Qualification> Qualifications => Set<Qualification>();

    public DbSet<QualificationField> QualificationFields => Set<QualificationField>();

    public DbSet<Funder> Funders => Set<Funder>();

    public DbSet<Bursary> Bursaries => Set<Bursary>();

    public DbSet<Source> Sources => Set<Source>();

    public DbSet<BursaryField> BursaryFields => Set<BursaryField>();

    public DbSet<BursaryQualification> BursaryQualifications => Set<BursaryQualification>();

    public DbSet<BursaryInstitution> BursaryInstitutions => Set<BursaryInstitution>();

    public DbSet<BursarySource> BursarySources => Set<BursarySource>();

    public DbSet<RawListing> RawListings => Set<RawListing>();

    public DbSet<IngestionRun> IngestionRuns => Set<IngestionRun>();

    public DbSet<IngestionRunSource> IngestionRunSources => Set<IngestionRunSource>();

    public DbSet<IngestionRunRequest> IngestionRunRequests => Set<IngestionRunRequest>();

    public DbSet<ListingReport> ListingReports => Set<ListingReport>();

    public DbSet<ReviewItem> ReviewItems => Set<ReviewItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        builder.HasPostgresExtension("pg_trgm");
        builder.ApplyConfigurationsFromAssembly(typeof(GetABursaryDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    private void ApplyAuditTimestamps()
    {
        var now = clock.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}
