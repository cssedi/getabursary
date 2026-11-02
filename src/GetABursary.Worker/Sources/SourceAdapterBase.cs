using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using GetABursary.Core.Common;
using GetABursary.Worker.Sources.Http;

namespace GetABursary.Worker.Sources;

/// <summary>Shared helpers for adapters.</summary>
public abstract partial class SourceAdapterBase : ISourceAdapter
{
    public const int MaxTextLength = 8000;

    public abstract string SourceKey { get; }

    public abstract IAsyncEnumerable<Uri> DiscoverAsync(IPageFetcher fetcher, SourceOptions options, CancellationToken ct);

    public abstract Result<RawListing> Parse(Uri url, string html);

    protected static IHtmlDocument ParseDocument(string html) => new HtmlParser().ParseDocument(html);

    /// <summary>Text of an element with scripts/styles removed and whitespace collapsed.</summary>
    protected static string CleanText(IElement? element)
    {
        if (element is null)
        {
            return string.Empty;
        }

        var clone = (IElement)element.Clone(true);
        foreach (var noise in clone.QuerySelectorAll("script, style, noscript").ToList())
        {
            noise.Remove();
        }

        return Whitespace().Replace(clone.TextContent, " ").Trim();
    }

    protected static string Truncate(string text, int maxLength = MaxTextLength)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}
