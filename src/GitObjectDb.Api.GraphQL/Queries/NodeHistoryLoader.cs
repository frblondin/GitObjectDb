using GitDotNet;
using GitObjectDb.Api.GraphQL.Tools;
using GraphQL;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GitObjectDb.Api.GraphQL.Queries;

internal class NodeHistoryLoader(IConnection queryAccessor,
                                IMemoryCache memoryCache,
                                IOptions<GitObjectDbGraphQLOptions> options)
    : CachedResultLoaderBase<NodeHistoryQueryKey, IEnumerable<CommitEntry>>(memoryCache, options)
{
    protected override async Task<IEnumerable<CommitEntry>> FetchAsync(ICacheEntry cacheEntry, NodeHistoryQueryKey key) =>
        queryAccessor
            .GetLogsAsync(await key.Context.GetCommitAsync(), key.Node)
            .SelectAwait(async entry => await entry.GetCommitAsync())
            .ToEnumerable();
}

internal record NodeHistoryQueryKey(IResolveFieldContext Context)
{
    public Node Node { get; } = Context.Source as Node ?? throw new ExecutionError("No node could be found in context.");
}