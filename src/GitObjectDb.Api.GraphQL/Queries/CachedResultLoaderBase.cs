using GraphQL.DataLoader;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GitObjectDb.Api.GraphQL.Queries;

internal abstract class CachedResultLoaderBase<TKey, TResult>(IMemoryCache memoryCache,
                                                              IOptions<GitObjectDbGraphQLOptions> options)
    : DataLoaderBase<TKey, TResult>(false)
    where TKey : notnull
{
    protected override Task FetchAsync(IEnumerable<DataLoaderPair<TKey, TResult>> list, CancellationToken cancellationToken)
    {
        foreach (var loadPair in list)
        {
            var result = memoryCache.GetOrCreate(loadPair.Key!, entry =>
            {
                options.Value.CacheEntryStrategy(entry);

                return Fetch(entry, loadPair.Key);
            });
            loadPair.SetResult(result!);
        }

        return Task.CompletedTask;
    }

    protected abstract TResult Fetch(ICacheEntry cacheEntry, TKey key);
}