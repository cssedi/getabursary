using System.Globalization;
using System.Text.RegularExpressions;

namespace GetABursary.Worker.Sources.Parsing;

/// <summary>Parses the date formats South African bursary sites use. English month names, invariant culture.</summary>
public static partial class SaDateParser
{
    private static readonly string[] Formats =
    [
        "d MMMM yyyy", "d MMM yyyy", "d/M/yyyy", "yyyy-M-d", "MMMM d, yyyy", "MMM d, yyyy", "MMMM d yyyy", "MMM d yyyy",
    ];

    public static bool TryParse(string? text, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (Match m in Candidates().Matches(text))
        {
            var candidate = Ordinals().Replace(m.Value, "$1");
            candidate = Regex.Replace(candidate, @"\s+", " ").Replace("Sept ", "Sep ", StringComparison.OrdinalIgnoreCase);
            if (DateOnly.TryParseExact(candidate, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return true;
            }
        }

        date = default;
        return false;
    }

    [GeneratedRegex(@"\d{4}-\d{1,2}-\d{1,2}|\d{1,2}/\d{1,2}/\d{4}|\d{1,2}(?:st|nd|rd|th)?\s+[A-Za-z]{3,9},?\s+\d{4}|[A-Za-z]{3,9}\s+\d{1,2}(?:st|nd|rd|th)?,?\s+\d{4}", RegexOptions.CultureInvariant)]
    private static partial Regex Candidates();

    [GeneratedRegex(@"(\d)(?:st|nd|rd|th)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Ordinals();
}
