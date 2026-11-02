using System.Globalization;
using System.Text;

namespace GetABursary.Core.Domain.Bursaries;

public static class SlugGenerator
{
    private const int MaxNameLength = 180;

    /// <summary>Returns <c>kebab-case-name-&lt;first 6 hex chars of id&gt;</c>.</summary>
    public static string Create(string name, Guid id)
    {
        var suffix = id.ToString("N")[..6];
        var kebab = Kebab(name);
        return kebab.Length == 0 ? suffix : $"{kebab}-{suffix}";
    }

    private static string Kebab(string? name)
    {
        var sb = new StringBuilder();
        var pendingDash = false;
        foreach (var ch in (name ?? string.Empty).ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (ch is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                if (pendingDash && sb.Length > 0)
                {
                    sb.Append('-');
                }

                pendingDash = false;
                sb.Append(ch);
            }
            else if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark && ch != '\'' && ch != '’')
            {
                pendingDash = true;
            }
        }

        var result = sb.ToString();
        return result.Length > MaxNameLength ? result[..MaxNameLength].TrimEnd('-') : result;
    }
}
