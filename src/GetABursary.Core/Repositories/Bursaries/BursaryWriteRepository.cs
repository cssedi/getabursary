using GetABursary.Core.Data;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Ingestion;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Core.Repositories.Bursaries;

public class BursaryWriteRepository(GetABursaryDbContext db) : IBursaryWriteRepository
{
    public async Task<IReadOnlyList<BursaryCandidate>> FindCandidatesAsync(
        string normalizedName, double minSimilarity, CancellationToken ct) =>
        await db.Bursaries.AsNoTracking()
            .Select(b => new
            {
                b.Id,
                b.FunderId,
                b.ClosingDate,
                Similarity = EF.Functions.TrigramsSimilarity(b.NormalizedName, normalizedName),
            })
            .Where(c => c.Similarity >= minSimilarity)
            .OrderByDescending(c => c.Similarity)
            .Select(c => new BursaryCandidate(c.Id, c.FunderId, c.ClosingDate, c.Similarity))
            .ToListAsync(ct);

    public Task<Bursary?> GetForMergeAsync(Guid id, CancellationToken ct) =>
        db.Bursaries
            .Include(b => b.Fields)
            .Include(b => b.Qualifications)
            .Include(b => b.Institutions)
            .Include(b => b.Sources)
            .AsSplitQuery()
            .SingleOrDefaultAsync(b => b.Id == id, ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct) =>
        db.Bursaries.AsNoTracking().AnyAsync(b => b.Slug == slug, ct);

    public Task<RawListing?> GetRawListingAsync(Guid rawListingId, CancellationToken ct) =>
        db.RawListings.SingleOrDefaultAsync(r => r.Id == rawListingId, ct);

    public async Task<IReadOnlyList<Guid>> GetFieldIdsByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken ct)
    {
        var list = codes.ToArray();
        if (list.Length == 0)
        {
            return [];
        }

        return await db.FieldsOfStudy.AsNoTracking().Where(f => list.Contains(f.Code)).Select(f => f.Id).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> GetQualificationIdsAsync(
        IReadOnlyCollection<string> qualificationNames,
        IReadOnlyCollection<string> institutionShortNames,
        CancellationToken ct)
    {
        var names = qualificationNames.Select(n => n.Trim().ToLowerInvariant()).ToArray();
        var shorts = institutionShortNames.Select(n => n.Trim().ToLowerInvariant()).ToArray();
        if (names.Length == 0 || shorts.Length == 0)
        {
            return [];
        }

        return await db.Qualifications.AsNoTracking()
#pragma warning disable CA1304, CA1311 // translated to SQL lower(); the lists above are invariant-lowercased
            .Where(q => names.Contains(q.Name.ToLower()) && shorts.Contains(q.Faculty.Institution.ShortName.ToLower()))
#pragma warning restore CA1304, CA1311
            .Select(q => q.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> GetInstitutionIdsByShortNamesAsync(
        IReadOnlyCollection<string> shortNames, CancellationToken ct)
    {
        var shorts = shortNames.Select(n => n.Trim().ToLowerInvariant()).ToArray();
        if (shorts.Length == 0)
        {
            return [];
        }

        return await db.Institutions.AsNoTracking()
#pragma warning disable CA1304, CA1311 // translated to SQL lower(); the list above is invariant-lowercased
            .Where(i => shorts.Contains(i.ShortName.ToLower()))
#pragma warning restore CA1304, CA1311
            .Select(i => i.Id)
            .ToListAsync(ct);
    }

    public void Add(Bursary bursary) => db.Bursaries.Add(bursary);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
