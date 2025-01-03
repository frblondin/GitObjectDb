using GraphQL.Types;
using Microsoft.Extensions.Caching.Memory;

namespace GitObjectDb.Api.GraphQL;

/// <summary>Provides a storage strategy of an entry stored in the cache.</summary>
/// <param name="entry">The cache entry to be stored in the cache.</param>
public delegate void CacheEntryStrategyProvider(ICacheEntry entry);

/// <summary>Used to configure additional GraphQL behavior.</summary>
public class GitObjectDbGraphQLOptions
{
    private CacheEntryStrategyProvider? _cacheEntryStrategy;

    /// <summary>
    /// Gets or sets the <see cref="CacheEntryStrategyProvider"/> strategy for all objects stored in the cache.
    /// </summary>
    public CacheEntryStrategyProvider CacheEntryStrategy
    {
        get => _cacheEntryStrategy ??
            throw new NotSupportedException($"The {nameof(CacheEntryStrategy)} cannot be null.");
        set => _cacheEntryStrategy = value;
    }

    /// <summary>Gets or sets the additional configuration to be applied to the schema used by GraphQL.</summary>
    public Action<ISchema>? ConfigureSchema { get; set; }
}