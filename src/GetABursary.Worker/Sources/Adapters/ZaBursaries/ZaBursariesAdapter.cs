using System.Runtime.CompilerServices;
using AngleSharp.Dom;
using GetABursary.Core.Common;
using GetABursary.Worker.Sources.Http;
using GetABursary.Worker.Sources.Parsing;

namespace GetABursary.Worker.Sources.Adapters.ZaBursaries;

public sealed class ZaBursariesAdapter : SourceAdapterBase
{
    private static readonly Error NotABursary = Error.Validation("Parse.NotABursary", "The page does not look like a bursary listing");

    public override string SourceKey => "zabursaries";

    public override async IAsyncEnumerable<Uri> DiscoverAsync(
        IPageFetcher fetcher,
        SourceOptions options,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fetcher);
        ArgumentNullException.ThrowIfNull(options);

        var seen = new HashSet<Uri>();
        Uri? next = options.BaseUrl;
        for (var page = 0; page < options.MaxPages && next is not null; page++)
        {
            var fetched = await fetcher.GetAsync(next, options.CrawlDelayMs, ct);
            if (fetched.IsFailure)
            {
                yield break;
            }

            var doc = ParseDocument(fetched.Value.Html);
            foreach (var link in doc.QuerySelectorAll(ZaBursariesSelectors.IndexLinks))
            {
                if (IsListing(Resolve(fetched.Value.Url, link.GetAttribute("href")), options.BaseUrl) is { } url && seen.Add(url))
                {
                    yield return url;
                    if (seen.Count >= options.MaxListings)
                    {
                        yield break;
                    }
                }
            }

            var nextHref = doc.QuerySelector(ZaBursariesSelectors.NextPage)?.GetAttribute("href");
            var nextUrl = Resolve(fetched.Value.Url, nextHref);
            next = nextUrl is not null && nextUrl != fetched.Value.Url ? nextUrl : null;
        }
    }

    public override Result<RawListing> Parse(Uri url, string html)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(html);

        var doc = ParseDocument(html);
        var content = doc.QuerySelector(ZaBursariesSelectors.Content);
        var title = CleanText(doc.QuerySelector(ZaBursariesSelectors.Title));
        if (content is null || title.Length == 0)
        {
            return NotABursary;
        }

        var sections = Sections(content);
        var eligibility = Join(sections, ZaBursariesSelectors.EligibilityHeading);
        if (eligibility is null)
        {
            return NotABursary;
        }

        return new RawListing(
            url,
            title,
            FunderName(sections),
            Date(sections, ZaBursariesSelectors.ClosingHeading),
            Date(sections, ZaBursariesSelectors.OpeningHeading),
            ApplyUrl(sections, url),
            Truncate(eligibility),
            Join(sections, ZaBursariesSelectors.FieldsHeading) is { } fields ? Truncate(fields) : null,
            Join(sections, ZaBursariesSelectors.ValueHeading) is { } value ? Truncate(value) : null);
    }

    private static Uri? Resolve(Uri baseUrl, string? href) =>
        Uri.TryCreate(baseUrl, href, out var uri) && uri.Scheme is "http" or "https"
            ? new UriBuilder(uri) { Fragment = string.Empty, Query = string.Empty }.Uri
            : null;

    private static Uri? IsListing(Uri? url, Uri baseUrl)
    {
        if (url is null || !string.Equals(url.Host, baseUrl.Host, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = url.AbsolutePath.Trim('/').Split('/');
        return segments.Length == 2 && segments[0].EndsWith(ZaBursariesSelectors.ListingPathSuffix, StringComparison.Ordinal) ? url : null;
    }

    /// <summary>Splits the article body into (heading, content elements) at each h2/h3.</summary>
    private static List<(string Heading, List<IElement> Body)> Sections(IElement content)
    {
        var sections = new List<(string, List<IElement>)>();
        foreach (var child in content.Children)
        {
            if (child.Matches(ZaBursariesSelectors.Headings))
            {
                sections.Add((CleanText(child), []));
            }
            else if (sections.Count > 0)
            {
                sections[^1].Item2.Add(child);
            }
        }

        return sections;
    }

    private static bool Is((string Heading, List<IElement> Body) section, string[] keywords) =>
        keywords.Any(k => section.Heading.Contains(k, StringComparison.OrdinalIgnoreCase));

    private static string? Join(List<(string Heading, List<IElement> Body)> sections, string[] keywords)
    {
        var text = string.Join(' ', sections.Where(s => Is(s, keywords)).SelectMany(s => s.Body).Select(CleanText)).Trim();
        return text.Length == 0 ? null : text;
    }

    private static DateOnly? Date(List<(string Heading, List<IElement> Body)> sections, string[] keywords) =>
        Join(sections, keywords) is { } text && SaDateParser.TryParse(text, out var date) ? date : null;

    private static string? FunderName(List<(string Heading, List<IElement> Body)> sections)
    {
        var heading = sections.Select(s => s.Heading)
            .FirstOrDefault(h => h.StartsWith(ZaBursariesSelectors.ProviderHeadingPrefix, StringComparison.OrdinalIgnoreCase));
        var name = heading?[ZaBursariesSelectors.ProviderHeadingPrefix.Length..].Trim(' ', '-', '–', '—');
        return string.IsNullOrEmpty(name) ? null : name;
    }

    /// <summary>First external link in the "how to apply" section.</summary>
    private static Uri? ApplyUrl(List<(string Heading, List<IElement> Body)> sections, Uri pageUrl) =>
        sections.Where(s => Is(s, ZaBursariesSelectors.ApplyHeading))
            .SelectMany(s => s.Body)
            .SelectMany(e => e.QuerySelectorAll(ZaBursariesSelectors.Links))
            .Select(a => Uri.TryCreate(a.GetAttribute("href"), UriKind.Absolute, out var u) && u.Scheme is "http" or "https" ? u : null)
            .FirstOrDefault(u => u is not null && !string.Equals(u.Host, pageUrl.Host, StringComparison.OrdinalIgnoreCase));
}
