using System.Runtime.CompilerServices;
using AngleSharp.Dom;
using GetABursary.Core.Common;
using GetABursary.Worker.Sources.Http;
using GetABursary.Worker.Sources.Parsing;

namespace GetABursary.Worker.Sources.Adapters.StudyTrust;

/// <summary>https://studytrust.org.za: one index page of bursary tiles, one Divi-built page per bursary.</summary>
public sealed class StudyTrustAdapter : SourceAdapterBase
{
    private static readonly Error NotABursary =
        Error.Validation("Parse.NotABursary", "The page has no bursary title or eligibility section");

    public override string SourceKey => "studytrust";

    public override async IAsyncEnumerable<Uri> DiscoverAsync(
        IPageFetcher fetcher, SourceOptions options, [EnumeratorCancellation] CancellationToken ct)
    {
        // shortcut: the index is a single page with no pagination; revisit if StudyTrust adds paging.
        var indexUrl = new Uri(options.BaseUrl, StudyTrustSelectors.IndexPath);
        var page = await fetcher.GetAsync(indexUrl, options.CrawlDelayMs, ct);
        if (page.IsFailure)
        {
            yield break;
        }

        var seen = new HashSet<Uri>();
        foreach (var link in ParseDocument(page.Value.Html).QuerySelectorAll(StudyTrustSelectors.IndexListingLinks))
        {
            if (seen.Count >= options.MaxListings)
            {
                yield break;
            }

            if (Uri.TryCreate(indexUrl, link.GetAttribute("href"), out var url)
                && url.Host == options.BaseUrl.Host
                && url.AbsolutePath.Trim('/').Length > 0
                && url != indexUrl
                && seen.Add(url))
            {
                yield return url;
            }
        }
    }

    public override Result<RawListing> Parse(Uri url, string html)
    {
        var doc = ParseDocument(html);
        foreach (var br in doc.QuerySelectorAll("br").ToList())
        {
            br.Replace(doc.CreateTextNode(" "));
        }

        var title = CleanText(doc.QuerySelector(StudyTrustSelectors.Title));

        var sections = new List<(string Heading, string Body)>();
        foreach (var row in doc.QuerySelectorAll(StudyTrustSelectors.SectionRow))
        {
            var heading = CleanText(row.QuerySelector(StudyTrustSelectors.SectionHeading));
            var body = CleanText(row.QuerySelector(StudyTrustSelectors.SectionBody));
            if (heading.Length > 0 && body.Length > 0)
            {
                sections.Add((heading, body));
            }
        }

        var fields = Join(sections, StudyTrustSelectors.FieldsHeadings);
        var eligibility = Join(sections, StudyTrustSelectors.EligibilityHeadings, exclude: StudyTrustSelectors.FieldsHeadings);
        if (title.Length == 0 || eligibility is null)
        {
            return Result<RawListing>.Failure(NotABursary);
        }

        // shortcut: only dates written with a year are read (the site often says just "01 June to 30 September"); upgrade if a year can be inferred.
        var dates = Join(sections, StudyTrustSelectors.DateHeadings);
        DateOnly? closing = null;
        DateOnly? opening = null;
        if (dates is not null)
        {
            // "01 June 2026 to 11 November 2026": the part before " to " opens, the rest closes.
            var split = dates.IndexOf(" to ", StringComparison.OrdinalIgnoreCase);
            if (split > 0 && SaDateParser.TryParse(dates[..split], out var from))
            {
                opening = from;
            }

            if (SaDateParser.TryParse(split > 0 ? dates[(split + 4)..] : dates, out var to))
            {
                closing = to;
            }
        }

        // shortcut: FunderName is left null; the page rarely names the funder outside free text.
        return Result<RawListing>.Success(new RawListing(
            url, title, null, closing, opening, FindApplyUrl(doc, url),
            Truncate(eligibility), fields is null ? null : Truncate(fields), Join(sections, StudyTrustSelectors.ValueHeadings)));
    }

    private static Uri? FindApplyUrl(IDocument doc, Uri pageUrl)
    {
        foreach (var button in doc.QuerySelectorAll(StudyTrustSelectors.ApplyButton))
        {
            if (Uri.TryCreate(pageUrl, button.GetAttribute("href"), out var target)
                && target.Scheme is "http" or "https"
                && target.Host != pageUrl.Host)
            {
                return target;
            }
        }

        return null;
    }

    private static string? Join(List<(string Heading, string Body)> sections, string[] keywords, string[]? exclude = null)
    {
        var matches = sections
            .Where(s => Matches(s.Heading, keywords) && (exclude is null || !Matches(s.Heading, exclude)))
            .Select(s => s.Body)
            .ToList();
        return matches.Count == 0 ? null : string.Join("\n\n", matches);
    }

    private static bool Matches(string heading, string[] keywords) =>
        keywords.Any(k => heading.Contains(k, StringComparison.OrdinalIgnoreCase));
}
