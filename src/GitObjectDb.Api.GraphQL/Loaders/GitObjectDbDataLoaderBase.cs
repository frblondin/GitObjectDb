using GraphQL.DataLoader;
using Microsoft.Extensions.Caching.Memory;

namespace GitObjectDb.Api.GraphQL.Loaders;

internal abstract class GitObjectDbDataLoaderBase<TKey, TResult> : DataLoaderBase<TKey, TResult>
{
    private readonly IMemoryCache _memoryCache;
    private readonly CacheEntryStrategyProvider _cacheStrategy;

    protected GitObjectDbDataLoaderBase(IMemoryCache memoryCache, CacheEntryStrategyProvider cacheStrategy)
        : base(false)
    {
        _memoryCache = memoryCache;
        _cacheStrategy = cacheStrategy;
    }

    protected override Task FetchAsync(IEnumerable<DataLoaderPair<TKey, TResult>> list, CancellationToken cancellationToken)
    {
        foreach (var loadPair in list)
        {
            var result = _memoryCache.GetOrCreate(loadPair.Key, entry =>
            {
                _cacheStrategy(entry);

                return Fetch(entry, loadPair.Key);
            });
            loadPair.SetResult(result);
        }

        return Task.CompletedTask;
    }

    protected abstract TResult Fetch(ICacheEntry cacheEntry, TKey key);
}