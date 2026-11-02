using System.Text.RegularExpressions;

namespace GetABursary.Core.Domain.Bursaries;

/// <summary>Produces the normalised name used for funder and bursary matching (spec 02 §4.1).</summary>
public static partial class NameNormalizer
{
    /// <summary>
    /// Lowercases, removes year tokens (<c>20\d\d</c>), the words bursary/bursaries/programme/program/scheme/scholarship,
    /// punctuation and repeated whitespace.
    /// </summary>
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var text = name.ToLowerInvariant();
        text = YearRegex().Replace(text, " ");
        text = StopWordRegex().Replace(text, " ");
        text = Apostrophes().Replace(text, string.Empty);
        text = Punctuation().Replace(text, " ");
        return Whitespace().Replace(text, " ").Trim();
    }

    [GeneratedRegex(@"\b20\d\d\b", RegexOptions.CultureInvariant)]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"\b(?:bursary|bursaries|programme|program|scheme|scholarship)\b", RegexOptions.CultureInvariant)]
    private static partial Regex StopWordRegex();

    [GeneratedRegex(@"['’‘`]", RegexOptions.CultureInvariant)]
    private static partial Regex Apostrophes();

    [GeneratedRegex(@"[\p{P}\p{S}]", RegexOptions.CultureInvariant)]
    private static partial Regex Punctuation();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}
