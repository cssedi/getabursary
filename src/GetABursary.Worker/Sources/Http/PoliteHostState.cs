using System.Collections.Concurrent;

namespace GetABursary.Worker.Sources.Http;

/// <summary>
/// Process-wide per-host state (robots.txt cache and last request time). Registered as a singleton
/// because typed <see cref="HttpClient"/>s are transient.
/// </summary>
public sealed class PoliteHostState
{
    private readonly ConcurrentDictionary<string, HostEntry> _hosts = new(StringComparer.OrdinalIgnoreCase);

    internal HostEntry For(Uri url) => _hosts.GetOrAdd($"{url.Scheme}://{url.Authority}", _ => new HostEntry());

    internal sealed class HostEntry
    {
        /// <summary>Serialises request starts to this host.</summary>
        public SemaphoreSlim Throttle { get; } = new(1, 1);

        public DateTimeOffset? LastRequestStart { get; set; }

        /// <summary>Serialises robots.txt loading for this host.</summary>
        public SemaphoreSlim RobotsLock { get; } = new(1, 1);

        public RobotsRules? Rules { get; set; }

        public DateTimeOffset RulesExpiry { get; set; }
    }
}
