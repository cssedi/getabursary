using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Ingestion;
using GetABursary.Core.Repositories.Bursaries;
using GetABursary.Core.Repositories.Ingestion;
using GetABursary.Core.Repositories.Reports;
using GetABursary.Worker.Pipeline.Jobs;
using GetABursary.Worker.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Worker.Tests.Pipeline;

[Collection("Postgres")]
public class ClosureJobTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 6, 10, 10, 0, 0, TimeSpan.Zero);

    private async Task<Guid> AddBursaryAsync(string name, DateOnly? closing, bool published, BursaryStatus status)
    {
        await using var db = fixture.CreateContext();
        var id = Guid.CreateVersion7();
        db.Bursaries.Add(new Bursary
        {
            Id = id,
            Slug = $"closure-{id:N}",
            Name = name,
            NormalizedName = name.ToLowerInvariant(),
            ClosingDate = closing,
            IsPublished = published,
            Status = status,
        });
        await db.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task RunAsync_PublishedBursaryPastClosingDate_IsClosed()
    {
        var expired = await AddBursaryAsync("Expired", new DateOnly(2026, 6, 9), published: true, BursaryStatus.Open);
        var closesToday = await AddBursaryAsync("Today", new DateOnly(2026, 6, 10), published: true, BursaryStatus.Open);
        var future = await AddBursaryAsync("Future", new DateOnly(2026, 7, 1), published: true, BursaryStatus.Open);
        var noDate = await AddBursaryAsync("No date", null, published: true, BursaryStatus.Unknown);
        var draft = await AddBursaryAsync("Draft", new DateOnly(2026, 6, 1), published: false, BursaryStatus.Open);

        await using (var db = fixture.CreateContext())
        {
            var job = new ClosureJob(new BursaryWriteRepository(db), new FakeClock(Now));
            Assert.True(await job.RunAsync(CancellationToken.None) >= 1);
        }

        await using var check = fixture.CreateContext();
        async Task<BursaryStatus> StatusOf(Guid id) => (await check.Bursaries.AsNoTracking().SingleAsync(b => b.Id == id)).Status;
        Assert.Equal(BursaryStatus.Closed, await StatusOf(expired));
        Assert.Equal(BursaryStatus.Open, await StatusOf(closesToday));
        Assert.Equal(BursaryStatus.Open, await StatusOf(future));
        Assert.Equal(BursaryStatus.Unknown, await StatusOf(noDate));
        Assert.Equal(BursaryStatus.Open, await StatusOf(draft));
    }

    [Fact]
    public async Task RunAsync_UsesSastDate_NotUtc()
    {
        // 23:30 UTC on 9 June is already 10 June in SAST (UTC+2), so a bursary closing on 9 June is closed.
        var id = await AddBursaryAsync("Sast", new DateOnly(2026, 6, 9), published: true, BursaryStatus.Open);
        var late = new SastClock(new DateTimeOffset(2026, 6, 9, 23, 30, 0, TimeSpan.Zero));

        await using (var db = fixture.CreateContext())
        {
            await new ClosureJob(new BursaryWriteRepository(db), late).RunAsync(CancellationToken.None);
        }

        await using var check = fixture.CreateContext();
        Assert.Equal(BursaryStatus.Closed, (await check.Bursaries.AsNoTracking().SingleAsync(b => b.Id == id)).Status);
    }

    private sealed class SastClock(DateTimeOffset now) : GetABursary.Core.Common.IClock
    {
        public DateTimeOffset UtcNow => now;

        public DateOnly Today => DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(2)).DateTime);
    }
}

[Collection("Postgres")]
public class VanishedJobTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 1, 3, 0, 0, TimeSpan.Zero);

    private sealed record Seeded(Guid SourceId, Guid RawListingId, Guid BursaryId, string Url);

    private async Task<Seeded> SeedAsync(Guid? otherSourceId = null, DateTimeOffset? otherSeenAt = null)
    {
        await using var db = fixture.CreateContext();
        var source = new Source { Key = "van-" + Guid.NewGuid().ToString("N"), Name = "Vanish", BaseUrl = "https://van.test" };
        var bursary = new Bursary
        {
            Slug = "van-" + Guid.NewGuid().ToString("N"),
            Name = "Vanishing bursary",
            NormalizedName = "vanishing bursary",
            IsPublished = true,
        };
        var url = $"https://van.test/{Guid.NewGuid():N}";
        var raw = new RawListing
        {
            SourceId = source.Id,
            Source = source,
            Url = url,
            ContentHash = new string('b', 64),
            Html = "<html></html>",
            BursaryId = bursary.Id,
            FirstFetchedAt = T0,
            LastFetchedAt = T0,
            LastChangedAt = T0,
        };
        bursary.Sources.Add(new BursarySource { BursaryId = bursary.Id, SourceId = source.Id, Source = source, SourceUrl = url, FirstSeenAt = T0, LastSeenAt = T0 });
        if (otherSourceId is { } other)
        {
            bursary.Sources.Add(new BursarySource
            {
                BursaryId = bursary.Id,
                SourceId = other,
                SourceUrl = "https://other.test/x",
                FirstSeenAt = T0,
                LastSeenAt = otherSeenAt ?? T0,
            });
        }

        db.Add(source);
        db.Add(bursary);
        db.Add(raw);
        await db.SaveChangesAsync();
        return new Seeded(source.Id, raw.Id, bursary.Id, url);
    }

    private async Task<Guid> CreateOtherSourceAsync()
    {
        await using var db = fixture.CreateContext();
        var other = new Source { Key = "oth-" + Guid.NewGuid().ToString("N"), Name = "Other", BaseUrl = "https://other.test" };
        db.Sources.Add(other);
        await db.SaveChangesAsync();
        return other.Id;
    }

    private async Task<int> RunJobAsync(Guid sourceId, DateTimeOffset now, params Guid[] seen)
    {
        await using var db = fixture.CreateContext();
        var job = new VanishedJob(
            new RawListingRepository(db), new BursaryWriteRepository(db), new ReviewItemRepository(db), new FakeClock(now));
        return await job.RunAsync(sourceId, seen, CancellationToken.None);
    }

    private async Task<(int MissedRuns, int Vanished)> StateAsync(Seeded s)
    {
        await using var db = fixture.CreateContext();
        var missed = (await db.RawListings.AsNoTracking().SingleAsync(r => r.Id == s.RawListingId)).MissedRuns;
        var items = await db.ReviewItems.CountAsync(r => r.BursaryId == s.BursaryId && r.Type == ReviewItemType.Vanished);
        return (missed, items);
    }

    [Fact]
    public async Task RunAsync_ListingMissedTwice_RaisesVanishedOnce()
    {
        var s = await SeedAsync();

        Assert.Equal(0, await RunJobAsync(s.SourceId, T0.AddDays(1)));
        Assert.Equal((1, 0), await StateAsync(s));

        Assert.Equal(1, await RunJobAsync(s.SourceId, T0.AddDays(2)));
        Assert.Equal((2, 1), await StateAsync(s));

        Assert.Equal(0, await RunJobAsync(s.SourceId, T0.AddDays(3)));
        Assert.Equal((3, 1), await StateAsync(s));
    }

    [Fact]
    public async Task RunAsync_ListingSeen_IsNotCounted()
    {
        var s = await SeedAsync();

        await RunJobAsync(s.SourceId, T0.AddDays(1));
        await RunJobAsync(s.SourceId, T0.AddDays(2), s.RawListingId);

        Assert.Equal((1, 0), await StateAsync(s));
    }

    [Fact]
    public async Task RunAsync_BursaryStillSeenAtAnotherSourceRecently_NoReviewItem()
    {
        var other = await CreateOtherSourceAsync();
        var s = await SeedAsync(other, T0.AddDays(1));

        await RunJobAsync(s.SourceId, T0.AddDays(1));
        Assert.Equal(0, await RunJobAsync(s.SourceId, T0.AddDays(2)));

        Assert.Equal((2, 0), await StateAsync(s));
    }

    [Fact]
    public async Task RunAsync_OtherSourceLastSeenOverSevenDaysAgo_RaisesVanished()
    {
        var other = await CreateOtherSourceAsync();
        var s = await SeedAsync(other, T0);

        await RunJobAsync(s.SourceId, T0.AddDays(8));
        Assert.Equal(1, await RunJobAsync(s.SourceId, T0.AddDays(9)));

        Assert.Equal((2, 1), await StateAsync(s));
    }
}
