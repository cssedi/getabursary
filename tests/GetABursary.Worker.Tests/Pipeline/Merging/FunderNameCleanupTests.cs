using GetABursary.Core.Domain.Bursaries;
using GetABursary.Worker.Pipeline.Merging;
using GetABursary.Worker.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Worker.Tests.Pipeline.Merging;

[Collection("Postgres")]
public class FunderNameCleanupTests(PostgresFixture fixture)
{
    private static Funder NewFunder(string name) => new() { Name = name, NormalizedName = NameNormalizer.Normalize(name) };

    private static Bursary NewBursary(string name, Guid? funderId) => new()
    {
        Slug = "cleanup-" + Guid.NewGuid().ToString("N"),
        Name = name,
        NormalizedName = NameNormalizer.Normalize(name),
        FunderId = funderId,
    };

    [Fact]
    public async Task RunAsync_DirtyFunders_RenamedOrMergedIntoExistingFunder()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var existing = NewFunder($"Clean{tag} Holdings");
        var collidingDirty = NewFunder($"Clean{tag} Holdings (administered in-house; [email protected])");
        var renameDirty = NewFunder($"Clean{tag} Renamed (applications via the portal)");
        var clean = NewFunder($"Clean{tag} Untouched");

        await using (var db = fixture.CreateContext())
        {
            db.Funders.AddRange(existing, collidingDirty, renameDirty, clean);
            await db.SaveChangesAsync();
            db.Bursaries.Add(NewBursary($"Clean{tag} Grant", collidingDirty.Id));
            db.Bursaries.Add(NewBursary($"Clean{tag} Prize", renameDirty.Id));
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext())
        {
            await new FunderNameCleanup(db).RunAsync(CancellationToken.None);
        }

        await using var check = fixture.CreateContext();
        Assert.False(await check.Funders.AnyAsync(f => f.Id == collidingDirty.Id));
        var grant = await check.Bursaries.SingleAsync(b => b.Name == $"Clean{tag} Grant");
        Assert.Equal(existing.Id, grant.FunderId);

        var renamed = await check.Funders.SingleAsync(f => f.Id == renameDirty.Id);
        Assert.Equal($"Clean{tag} Renamed", renamed.Name);
        Assert.Equal($"clean{tag} renamed", renamed.NormalizedName);
        Assert.Equal(renameDirty.Id, (await check.Bursaries.SingleAsync(b => b.Name == $"Clean{tag} Prize")).FunderId);

        Assert.Equal($"Clean{tag} Untouched", (await check.Funders.SingleAsync(f => f.Id == clean.Id)).Name);
    }
}
