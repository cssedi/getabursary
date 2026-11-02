using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GetABursary.Core.Common;
using GetABursary.Core.Domain.Ingestion;
using GetABursary.Core.Repositories.Ingestion;

namespace GetABursary.Worker.Pipeline;

public sealed partial class SnapshotStore(IRawListingRepository repository, IClock clock) : ISnapshotStore
{
    public async Task<SnapshotResult> SaveAsync(Guid sourceId, Uri url, string html, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(html);

        var now = clock.UtcNow;
        var hash = ComputeHash(html);
        var key = url.AbsoluteUri;
        var existing = await repository.GetBySourceAndUrlAsync(sourceId, key, ct);

        if (existing is null)
        {
            var created = new RawListing
            {
                SourceId = sourceId,
                Url = key,
                ContentHash = hash,
                Html = html,
                FirstFetchedAt = now,
                LastFetchedAt = now,
                LastChangedAt = now,
                MissedRuns = 0,
            };
            repository.Add(created);
            await repository.SaveChangesAsync(ct);
            return new SnapshotResult(created.Id, true);
        }

        var changed = !string.Equals(existing.ContentHash, hash, StringComparison.Ordinal);
        existing.LastFetchedAt = now;
        existing.MissedRuns = 0;
        if (changed)
        {
            existing.ContentHash = hash;
            existing.Html = html;
            existing.LastChangedAt = now;
        }

        await repository.SaveChangesAsync(ct);
        return new SnapshotResult(existing.Id, changed);
    }

    /// <summary>SHA-256 hex of the HTML with script blocks removed and whitespace runs collapsed.</summary>
    public static string ComputeHash(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        var withoutScripts = ScriptBlocks().Replace(html, string.Empty);
        var normalised = Whitespace().Replace(BetweenTags().Replace(withoutScripts, "><"), " ").Trim();
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalised)));
    }

    [GeneratedRegex(@"<script\b[^>]*>.*?</script\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex ScriptBlocks();

    [GeneratedRegex(@">\s+<", RegexOptions.CultureInvariant)]
    private static partial Regex BetweenTags();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}
