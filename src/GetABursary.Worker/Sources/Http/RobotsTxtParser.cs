using System.Globalization;
using System.Text.RegularExpressions;

namespace GetABursary.Worker.Sources.Http;

/// <summary>The rules that apply to our user agent, parsed from a robots.txt file.</summary>
public sealed class RobotsRules
{
    private readonly List<(Regex Pattern, int Length, bool Allow)> _rules;

    internal RobotsRules(List<(Regex Pattern, int Length, bool Allow)> rules, TimeSpan? crawlDelay)
    {
        _rules = rules;
        CrawlDelay = crawlDelay;
    }

    public static RobotsRules AllowAll { get; } = new([], null);

    public static RobotsRules DisallowAll { get; } = new([(new Regex("^/", RegexOptions.None, TimeSpan.FromSeconds(1)), 1, false)], null);

    public TimeSpan? CrawlDelay { get; }

    /// <summary>Longest matching pattern wins; Allow wins ties; no match means allowed.</summary>
    public bool IsAllowed(string pathAndQuery)
    {
        var bestLength = -1;
        var allowed = true;
        foreach (var (pattern, length, allow) in _rules)
        {
            if (!pattern.IsMatch(pathAndQuery))
            {
                continue;
            }

            if (length > bestLength || (length == bestLength && allow))
            {
                bestLength = length;
                allowed = allow;
            }
        }

        return allowed;
    }
}

public static class RobotsTxtParser
{
    /// <param name="content">The robots.txt body.</param>
    /// <param name="userAgentToken">Our product token, e.g. <c>GetABursaryBot</c>.</param>
    public static RobotsRules Parse(string content, string userAgentToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(userAgentToken);

        var groups = new List<Group>();
        Group? current = null;
        var lastWasAgent = false;

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine;
            var hash = line.IndexOf('#', StringComparison.Ordinal);
            if (hash >= 0)
            {
                line = line[..hash];
            }

            var colon = line.IndexOf(':', StringComparison.Ordinal);
            if (colon < 0)
            {
                continue;
            }

            var field = line[..colon].Trim().ToLowerInvariant();
            var value = line[(colon + 1)..].Trim();

            if (field == "user-agent")
            {
                if (!lastWasAgent || current is null)
                {
                    current = new Group();
                    groups.Add(current);
                }

                current.Agents.Add(value);
                lastWasAgent = true;
                continue;
            }

            lastWasAgent = false;
            if (current is null)
            {
                continue;
            }

            switch (field)
            {
                case "allow" or "disallow":
                    if (value.Length > 0)
                    {
                        current.Rules.Add((ToRegex(value), value.Length, field == "allow"));
                    }

                    break;
                case "crawl-delay":
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                        && seconds >= 0 && double.IsFinite(seconds))
                    {
                        current.CrawlDelay = TimeSpan.FromSeconds(seconds);
                    }

                    break;
            }
        }

        var specific = groups.Where(g => g.Agents.Any(a => a != "*" && a.Length > 0
            && userAgentToken.Contains(a, StringComparison.OrdinalIgnoreCase))).ToList();
        var chosen = specific.Count > 0 ? specific : groups.Where(g => g.Agents.Contains("*")).ToList();

        return new RobotsRules(
            chosen.SelectMany(g => g.Rules).ToList(),
            chosen.Select(g => g.CrawlDelay).FirstOrDefault(d => d is not null));
    }

    private static Regex ToRegex(string pattern)
    {
        var anchored = pattern.EndsWith('$');
        if (anchored)
        {
            pattern = pattern[..^1];
        }

        var body = string.Join(".*", pattern.Split('*').Select(Regex.Escape));
        return new Regex("^" + body + (anchored ? "$" : string.Empty), RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    }

    private sealed class Group
    {
        public List<string> Agents { get; } = [];

        public List<(Regex Pattern, int Length, bool Allow)> Rules { get; } = [];

        public TimeSpan? CrawlDelay { get; set; }
    }
}
