using GraphQL.DataLoader;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GitObjectDb.Api.GraphQL.Queries;

internal abstract class CachedResultLoaderBase<TKey, TResult>(IMemoryCache memoryCache,
                                                              IOptions<GitObjectDbGraphQLOptions> options)
    : DataLoaderBase<TKey, TResult>(false)
    where TKey : notnull
{
    protected override async Task FetchAsync(IEnumerable<DataLoaderPair<TKey, TResult>> list, CancellationToken cancellationToken)
    {
        foreach (var loadPair in list)
        {
            var result = await memoryCache.GetOrCreateAsync(loadPair.Key!, async entry =>
            {
                options.Value.CacheEntryStrategy(entry);

                return await FetchAsync(entry, loadPair.Key);
            });
            loadPair.SetResult(result!);
        }
    }

    protected abstract Task<TResult> FetchAsync(ICacheEntry cacheEntry, TKey key);
}