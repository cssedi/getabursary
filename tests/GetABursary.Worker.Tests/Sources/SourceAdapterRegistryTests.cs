using GetABursary.Core.Common;
using GetABursary.Worker.Sources;
using GetABursary.Worker.Sources.Http;

namespace GetABursary.Worker.Tests.Sources;

public class SourceAdapterRegistryTests
{
    private sealed class FakeAdapter(string key) : ISourceAdapter
    {
        public string SourceKey => key;

        public async IAsyncEnumerable<Uri> DiscoverAsync(IPageFetcher fetcher, SourceOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Result<RawListing> Parse(Uri url, string html) => throw new NotSupportedException();
    }

    [Fact]
    public void Get_KnownKey_ReturnsAdapter()
    {
        var adapter = new FakeAdapter("zabursaries");
        var registry = new SourceAdapterRegistry([adapter, new FakeAdapter("other")]);

        var result = registry.Get("zabursaries");

        Assert.True(result.IsSuccess);
        Assert.Same(adapter, result.Value);
    }

    [Fact]
    public void Get_UnknownKey_FailsWithAdapterNotFound()
    {
        var registry = new SourceAdapterRegistry([new FakeAdapter("zabursaries")]);

        var result = registry.Get("nope");

        Assert.False(result.IsSuccess);
        Assert.Equal("Source.AdapterNotFound", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Constructor_DuplicateKeys_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new SourceAdapterRegistry([new FakeAdapter("a"), new FakeAdapter("a")]));
    }
}
