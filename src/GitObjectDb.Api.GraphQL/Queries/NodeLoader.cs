using GitObjectDb.Api.GraphQL.Graph;
using GitObjectDb.Api.GraphQL.Tools;
using GraphQL;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GitObjectDb.Api.GraphQL.Queries;

internal class NodeLoader<TNode>(IConnection queryAccessor,
                                 IMemoryCache memoryCache,
                                 IOptions<GitObjectDbGraphQLOptions> options)
    : CachedResultLoaderBase<NodeDataLoaderKey, IEnumerable<Node>>(memoryCache, options)
    where TNode : Node
{
    protected override async Task<IEnumerable<Node>> FetchAsync(ICacheEntry cacheEntry, NodeDataLoaderKey key)
    {
        var commitId = await key.Context.GetCommitAsync();
        if (key.Id.HasValue)
        {
            var node = await queryAccessor.LookupAsync<TNode>(commitId, key.Id.Value);
            return node is null ? [] : [node];
        }
        else
        {
            var parent = key.ParentPath is not null ?
                await queryAccessor.LookupAsync<Node>(commitId, key.ParentPath) :
                null;
            var result = queryAccessor.GetNodesAsync<TNode>(commitId, parent, key.IsRecursive).ToEnumerable();
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
        IsRecursive = context.GetArgumentFromParentContexts(Query.IsRecursiveArgument, false);
        Id = context.GetArgument(Query.IdArgument, default(UniqueId?));
        Context = context;
    }

    public Node? ParentNode { get; }

    public DataPath? ParentPath { get; }

    public bool IsRecursive { get; }

    public UniqueId? Id { get; }

    public IResolveFieldContext Context { get; }
}