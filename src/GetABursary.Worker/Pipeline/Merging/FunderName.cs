using System.Text.RegularExpressions;

namespace GetABursary.Worker.Pipeline.Merging;

/// <summary>Turns scraped funder text such as "SRK Consulting (administered in-house; [email protected])" into a plain name.</summary>
public static partial class FunderName
{
    private static readonly string[] AdminMarkers = ["administered", "applications via", "apply via", "portal", "in-house", "@"];

    private static readonly char[] TrimChars = [' ', ',', ';', ':', '-', '—', '|', '.'];

    /// <summary>
    /// Removes Cloudflare email-protection text, bracketed admin notes and admin tails after a dash. Returns <c>null</c>
    /// for blank input and falls back to the trimmed original when cleaning would leave nothing.
    /// </summary>
    public static string? Clean(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var text = EmailProtection().Replace(name, string.Empty);
        text = Bracket().Replace(text, m => IsAdminNote(m.Groups[1].Value) ? string.Empty : m.Value);
        text = AdminTail().Replace(text, string.Empty);
        text = Spaces().Replace(text, " ").Trim(TrimChars);
        return text.Length == 0 ? name.Trim() : text;
    }

    private static bool IsAdminNote(string content) =>
        content.Trim().Length == 0 || AdminMarkers.Any(m => content.Contains(m, StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(@"\[email(?:&#160;| )protected\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailProtection();

    [GeneratedRegex(@"\s*[\(\[]([^\(\)\[\]]*)[\)\]]", RegexOptions.CultureInvariant)]
    private static partial Regex Bracket();

    [GeneratedRegex(@"\s+[—-]\s+(?:administered|applications)\b.*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AdminTail();

    [GeneratedRegex(@"\s{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex Spaces();
}
