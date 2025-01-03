using GitObjectDb.Api.GraphQL.Tools;
using GraphQL;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GitObjectDb.Api.GraphQL.Queries;

internal class NodeHistoryLoader(IQueryAccessor queryAccessor,
                                IMemoryCache memoryCache,
                                IOptions<GitObjectDbGraphQLOptions> options)
    : CachedResultLoaderBase<NodeHistoryQueryKey, IEnumerable<Commit>>(memoryCache, options)
{
    protected override IEnumerable<Commit> Fetch(ICacheEntry cacheEntry, NodeHistoryQueryKey key)
    {
        return queryAccessor
            .GetCommits(key.CommitId.Sha, key.Node)
            .Select(e => e.Commit);
    }
}

internal record NodeHistoryQueryKey(IResolveFieldContext Context)
{
    public Node Node { get; } = Context.Source as Node ?? throw new ExecutionError("No node could be found in context.");

    public ObjectId CommitId { get; } = Context.GetCommitId();
}