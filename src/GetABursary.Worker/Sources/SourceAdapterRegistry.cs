using GetABursary.Core.Common;

namespace GetABursary.Worker.Sources;

public static class SourceErrors
{
    public static Error AdapterNotFound(string key) =>
        Error.NotFound("Source.AdapterNotFound", $"No source adapter is registered for key '{key}'");
}

/// <summary>Looks adapters up by source key. Duplicate keys throw at construction (startup).</summary>
public sealed class SourceAdapterRegistry
{
    private readonly Dictionary<string, ISourceAdapter> _adapters = new(StringComparer.OrdinalIgnoreCase);

    public SourceAdapterRegistry(IEnumerable<ISourceAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        foreach (var adapter in adapters)
        {
            if (!_adapters.TryAdd(adapter.SourceKey, adapter))
            {
                throw new InvalidOperationException($"Duplicate source adapter key '{adapter.SourceKey}'.");
            }
        }
    }

    public IReadOnlyCollection<ISourceAdapter> All => _adapters.Values;

    public Result<ISourceAdapter> Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _adapters.TryGetValue(key, out var adapter)
            ? Result<ISourceAdapter>.Success(adapter)
            : Result<ISourceAdapter>.Failure(SourceErrors.AdapterNotFound(key));
    }
}
