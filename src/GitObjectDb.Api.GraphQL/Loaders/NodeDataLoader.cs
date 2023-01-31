using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Api.GraphQL.Tools;
using GraphQL;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GitObjectDb.Api.GraphQL.Loaders;

internal class NodeDataLoader<TNode> : GitObjectDbDataLoaderBase<NodeDataLoaderKey, IEnumerable<Node>>
    where TNode : Node
{
    private readonly IQueryAccessor _queryAccessor;

    public NodeDataLoader(IQueryAccessor queryAccessor, IMemoryCache memoryCache, IOptions<GitObjectDbGraphQLOptions> options)
        : base(memoryCache, options)
    {
        _queryAccessor = queryAccessor;
    }

    protected override IEnumerable<TNode> Fetch(ICacheEntry cacheEntry, NodeDataLoaderKey key)
    {
        if (key.Id.HasValue)
        {
            var node = _queryAccessor.Lookup<TNode>(key.CommitId.Sha, key.Id.Value);
            return node is null ? Array.Empty<TNode>() : new[] { node };
        }
        else
        {
            var parent = key.ParentPath is not null ?
                _queryAccessor.Lookup<Node>(key.CommitId.Sha, key.ParentPath) :
                null;
            var result = _queryAccessor.GetNodes<TNode>(key.CommitId.Sha, parent, key.IsRecursive);
            return result.ToList();
        }
    }
}

internal record NodeDataLoaderKey
{
    internal NodeDataLoaderKey(IResolveFieldContext context)
    {
        ParentNode = context.Source as Node;
        ParentPath = ParentNode?.Path ?? context.GetArgument(GitObjectDbQuery.ParentPathArgument, default(DataPath?));
        CommitId = context.GetCommitId();
        IsRecursive = context.GetArgumentFromParentContexts(GitObjectDbQuery.IsRecursiveArgument, false);
        Id = context.GetArgument(GitObjectDbQuery.IdArgument, default(UniqueId?));
    }

    public Node? ParentNode { get; }

    public DataPath? ParentPath { get; }

    public ObjectId CommitId { get; }

    public bool IsRecursive { get; }

    public UniqueId? Id { get; }
}