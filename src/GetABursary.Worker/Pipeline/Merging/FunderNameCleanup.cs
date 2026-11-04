using GetABursary.Core.Data;
using GetABursary.Core.Domain.Bursaries;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Worker.Pipeline.Merging;

public interface IFunderNameCleanup
{
    /// <summary>Cleans funder rows stored before <see cref="FunderName"/> existed. Returns how many rows changed.</summary>
    Task<int> RunAsync(CancellationToken ct);
}

/// <summary>
/// Renames dirty funders. When the cleaned name already exists, repoints their bursaries to that funder and deletes the dirty one.
/// shortcut: runs on every ingestion run and loads all funders; cheap because the funder table is small and later runs change nothing.
/// Upgrade: remove this step once every funder is clean.
/// </summary>
public sealed class FunderNameCleanup(GetABursaryDbContext db) : IFunderNameCleanup
{
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var funders = await db.Funders.ToListAsync(ct);
        var byNormalizedName = funders.ToDictionary(f => f.NormalizedName);
        var changed = 0;

        foreach (var dirty in funders)
        {
            var clean = FunderName.Clean(dirty.Name);
            var normalized = NameNormalizer.Normalize(clean);
            if (clean is null || clean == dirty.Name || normalized.Length == 0)
            {
                continue;
            }

            changed++;
            byNormalizedName.Remove(dirty.NormalizedName);
            if (byNormalizedName.TryGetValue(normalized, out var existing))
            {
                await db.Bursaries.Where(b => b.FunderId == dirty.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.FunderId, existing.Id), ct);
                db.Funders.Remove(dirty);
            }
            else
            {
                dirty.Name = clean;
                dirty.NormalizedName = normalized;
                byNormalizedName[normalized] = dirty;
            }
        }

        await db.SaveChangesAsync(ct);
        return changed;
    }
}
