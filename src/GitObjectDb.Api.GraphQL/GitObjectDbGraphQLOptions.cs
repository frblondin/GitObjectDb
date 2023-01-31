using GitObjectDb.Api.GraphQL.Loaders;
using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL;

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
