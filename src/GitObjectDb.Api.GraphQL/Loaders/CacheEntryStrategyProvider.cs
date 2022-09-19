using Microsoft.Extensions.Caching.Memory;

namespace GitObjectDb.Api.GraphQL.Loaders;

/// <summary>Provides a storage strategy of an entry stored in the cache.</summary>
/// <param name="entry">The cache entry to be stored in the cache.</param>
public delegate void CacheEntryStrategyProvider(ICacheEntry entry);
