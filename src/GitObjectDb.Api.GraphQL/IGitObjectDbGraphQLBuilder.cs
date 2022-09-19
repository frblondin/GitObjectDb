using GitObjectDb.Api.GraphQL.Loaders;
using GraphQL.Types;
using Microsoft.Extensions.Caching.Memory;

namespace GitObjectDb.Api.GraphQL;

/// <summary>Used to configure additional GraphQL behavior.</summary>
public interface IGitObjectDbGraphQLBuilder
{
    /// <summary>Gets the schema used by GraphQL.</summary>
    ISchema Schema { get; }

    /// <summary>
    /// Gets or sets the <see cref="CacheEntryStrategyProvider"/> strategy for all objects stored in the cache.
    /// </summary>
    CacheEntryStrategyProvider? CacheEntryStrategy { get; set; }
}

internal class GitObjectDbGraphQLBuilder : IGitObjectDbGraphQLBuilder
{
    public GitObjectDbGraphQLBuilder(ISchema schema)
    {
        Schema = schema;
    }

    public ISchema Schema { get; }

    public CacheEntryStrategyProvider? CacheEntryStrategy { get; set; }
}
