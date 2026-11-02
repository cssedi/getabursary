using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Ingestion;
using GetABursary.Core.Repositories.Ingestion;
using GetABursary.Core.Tests.Infrastructure;

namespace GetABursary.Core.Tests.Repositories.Ingestion;

[Collection("Postgres")]
public class RawListingRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task GetBySourceAndUrlAsync_AfterAdd_FindsOnlyMatchingSourceAndUrl()
    {
        var now = new DateTimeOffset(2026, 4, 1, 3, 0, 0, TimeSpan.Zero);
        var source = new Source { Key = "src-" + Guid.NewGuid().ToString("N"), Name = "Repo Source", BaseUrl = "https://repo.test" };
        await using (var db = fixture.CreateContext())
        {
            var repo = new RawListingRepository(db);
            repo.Add(new RawListing
            {
                Source = source, Url = "https://repo.test/a", ContentHash = new string('a', 64), Html = "<p>a</p>",
                FirstFetchedAt = now, LastFetchedAt = now, LastChangedAt = now,
            });
            await repo.SaveChangesAsync(CancellationToken.None);
        }

        await using var reader = fixture.CreateContext();
        var readRepo = new RawListingRepository(reader);
        var found = await readRepo.GetBySourceAndUrlAsync(source.Id, "https://repo.test/a", CancellationToken.None);
        var otherUrl = await readRepo.GetBySourceAndUrlAsync(source.Id, "https://repo.test/b", CancellationToken.None);
        var otherSource = await readRepo.GetBySourceAndUrlAsync(Guid.NewGuid(), "https://repo.test/a", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("<p>a</p>", found.Html);
        Assert.Null(otherUrl);
        Assert.Null(otherSource);
    }
}
