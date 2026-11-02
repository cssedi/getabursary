using GetABursary.Core.Repositories.Taxonomy;
using Microsoft.Extensions.DependencyInjection;

namespace GetABursary.Worker.Extraction;

/// <summary>The allowed CESM codes and names the extractor may use.</summary>
public interface IFieldCatalog
{
    Task<IReadOnlyDictionary<string, string>> GetCodesAsync(CancellationToken ct);
}

/// <summary>Loads fields of study once through a scope (the repository is scoped) and caches them.</summary>
public sealed class FieldCatalog(IServiceScopeFactory scopeFactory) : IFieldCatalog
{
    private IReadOnlyDictionary<string, string>? _cache;

    public async Task<IReadOnlyDictionary<string, string>> GetCodesAsync(CancellationToken ct)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFieldOfStudyRepository>();
        var fields = await repo.ListAllAsync(ct);
        _cache = fields.ToDictionary(f => f.Code, f => f.Name);
        return _cache;
    }
}
