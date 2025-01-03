using GitObjectDb.Api.GraphQL.Graph;
using GitObjectDb.Api.GraphQL.Tools;
using GraphQL;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GitObjectDb.Api.GraphQL.Queries;

internal class NodeLoader<TNode>(IQueryAccessor queryAccessor,
                                IMemoryCache memoryCache,
                                IOptions<GitObjectDbGraphQLOptions> options)
    : CachedResultLoaderBase<NodeDataLoaderKey, IEnumerable<Node>>(memoryCache, options)
    where TNode : Node
{
    protected override IEnumerable<TNode> Fetch(ICacheEntry cacheEntry, NodeDataLoaderKey key)
    {
        if (key.Id.HasValue)
        {
            var node = queryAccessor.Lookup<TNode>(key.CommitId.Sha, key.Id.Value);
            return node is null ? [] : [node];
        }
        else
        {
            var parent = key.ParentPath is not null ?
                queryAccessor.Lookup<Node>(key.CommitId.Sha, key.ParentPath) :
                null;
            var result = queryAccessor.GetNodes<TNode>(key.CommitId.Sha, parent, key.IsRecursive);
            return [.. result];
        }
    }
}

internal record NodeDataLoaderKey
{
    public NodeDataLoaderKey(IResolveFieldContext context)
    {
        ParentNode = context.Source as Node;
        ParentPath = ParentNode?.Path ?? context.GetArgument(Query.ParentPathArgument, default(DataPath?));
        CommitId = context.GetCommitId();
        IsRecursive = context.GetArgumentFromParentContexts(Query.IsRecursiveArgument, false);
        Id = context.GetArgument(Query.IdArgument, default(UniqueId?));
    }

    public Node? ParentNode { get; }

    public DataPath? ParentPath { get; }

    public ObjectId CommitId { get; }

    public bool IsRecursive { get; }

    public UniqueId? Id { get; }
}