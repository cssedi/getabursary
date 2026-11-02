using System.Runtime.CompilerServices;
using AngleSharp.Dom;
using GetABursary.Core.Common;
using GetABursary.Worker.Sources.Http;
using GetABursary.Worker.Sources.Parsing;

namespace GetABursary.Worker.Sources.Adapters.AllBursaries;

/// <summary>https://allbursaries.co.za: an index of field categories, each category lists bursary pages.</summary>
public sealed class AllBursariesAdapter : SourceAdapterBase
{
    private static readonly Error NotABursary =
        Error.Validation("Parse.NotABursary", "The page has no bursary title or eligibility section");

    public override string SourceKey => "allbursaries";

    public override async IAsyncEnumerable<Uri> DiscoverAsync(
        IPageFetcher fetcher, SourceOptions options, [EnumeratorCancellation] CancellationToken ct)
    {
        // MaxPages counts every page fetched: the index plus one page per category.
        var indexUrl = new Uri(options.BaseUrl, AllBursariesSelectors.IndexPath);
        var index = await fetcher.GetAsync(indexUrl, options.CrawlDelayMs, ct);
        if (index.IsFailure)
        {
            yield break;
        }

        var categories = Links(index.Value.Html, AllBursariesSelectors.IndexCategoryLinks, indexUrl, options, segments: 1)
            .Take(Math.Max(options.MaxPages - 1, 0))
            .ToList();

        var seen = new HashSet<Uri>();
        foreach (var category in categories)
        {
            var page = await fetcher.GetAsync(category, options.CrawlDelayMs, ct);
            if (page.IsFailure)
            {
                continue;
            }

            foreach (var url in Links(page.Value.Html, AllBursariesSelectors.CategoryListingLinks, category, options, segments: 2))
            {
                if (seen.Count >= options.MaxListings)
                {
                    yield break;
                }

                // Only /<this category>/<slug>; links to other sections are not listings.
                if (url.Segments[1].TrimEnd('/') == category.Segments[1] && seen.Add(url))
                {
                    yield return url;
                }
            }
        }
    }

    public override Result<RawListing> Parse(Uri url, string html)
    {
        var doc = ParseDocument(html);
        var title = CleanText(doc.QuerySelector(AllBursariesSelectors.Title));
        var eligibility = SectionText(doc, AllBursariesSelectors.EligibilityHeading);
        if (title.Length == 0 || eligibility is null)
        {
            return Result<RawListing>.Failure(NotABursary);
        }

        var funder = CleanText(doc.QuerySelector(AllBursariesSelectors.Funder));
        var fields = doc.QuerySelectorAll(AllBursariesSelectors.SectionHeading)
            .FirstOrDefault(h => Has(h, AllBursariesSelectors.FieldsHeading))
            ?.Closest(AllBursariesSelectors.SectionContainer)
            ?.QuerySelectorAll(AllBursariesSelectors.FieldItems)
            .Select(CleanText)
            .ToList();

        // The "Amount" line plus the "What's included" list are what the bursary covers.
        var value = string.Join("\n", new[] { Summary(doc, AllBursariesSelectors.AmountLabel), SectionText(doc, AllBursariesSelectors.ValueHeading) }
            .Where(s => !string.IsNullOrEmpty(s)));

        // Dates read "15 October 2026" or "To be announced"; anything SaDateParser can't read stays null.
        return Result<RawListing>.Success(new RawListing(
            url,
            title,
            funder.Length == 0 ? null : funder,
            ParseDate(Summary(doc, AllBursariesSelectors.DeadlineLabel)),
            ParseDate(Summary(doc, AllBursariesSelectors.OpenLabel)),
            FindApplyUrl(doc, url),
            Truncate(eligibility),
            fields is { Count: > 0 } ? Truncate(string.Join("\n", fields)) : null,
            value.Length == 0 ? null : Truncate(value)));
    }

    private static IEnumerable<Uri> Links(string html, string selector, Uri page, SourceOptions options, int segments) =>
        ParseDocument(html).QuerySelectorAll(selector)
            .Select(a => Uri.TryCreate(page, a.GetAttribute("href"), out var url) ? url : null)
            .Where(url => url is not null && url.Host == options.BaseUrl.Host && url.Segments.Length == segments + 1)
            .Select(url => new Uri(url!.GetLeftPart(UriPartial.Path).TrimEnd('/')))
            .Distinct();

    private static DateOnly? ParseDate(string? text) => SaDateParser.TryParse(text, out var date) ? date : null;

    private static bool Has(IElement element, string keyword) =>
        element.TextContent.Contains(keyword, StringComparison.OrdinalIgnoreCase);

    /// <summary>Value next to a summary label such as "Deadline".</summary>
    private static string? Summary(IDocument doc, string label)
    {
        var dd = doc.QuerySelectorAll(AllBursariesSelectors.SummaryLabel)
            .FirstOrDefault(dt => Has(dt, label))?.NextElementSibling;
        var text = CleanText(dd);
        return text.Length == 0 ? null : text;
    }

    /// <summary>The list items that follow a heading whose text contains <paramref name="keyword"/>.</summary>
    private static string? SectionText(IDocument doc, string keyword)
    {
        var list = doc.QuerySelectorAll(AllBursariesSelectors.SectionHeading)
            .FirstOrDefault(h => Has(h, keyword))?.NextElementSibling;
        var items = list?.QuerySelectorAll(AllBursariesSelectors.SectionItems).Select(CleanText).Where(t => t.Length > 0).ToList();
        return items is { Count: > 0 } ? string.Join("\n", items) : null;
    }

    private static Uri? FindApplyUrl(IDocument doc, Uri pageUrl)
    {
        foreach (var link in doc.QuerySelectorAll(AllBursariesSelectors.ApplyLink))
        {
            if (Uri.TryCreate(pageUrl, link.GetAttribute("href"), out var target)
                && target.Scheme is "http" or "https"
                && target.Host != pageUrl.Host)
            {
                return target;
            }
        }

        return null;
    }
}
