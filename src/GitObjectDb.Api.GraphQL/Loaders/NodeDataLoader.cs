using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Api.GraphQL.Tools;
using GitObjectDb.Api.Model;
using GraphQL;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;

namespace GitObjectDb.Api.GraphQL.Loaders;

internal class NodeDataLoader<TNode, TNodeDto> : GitObjectDbDataLoaderBase<NodeDataLoaderKey, IEnumerable<object?>>
    where TNode : Node
    where TNodeDto : NodeDto
{
    private readonly DataProvider _dataProvider;

    public NodeDataLoader(DataProvider dataProvider, IMemoryCache memoryCache, CacheEntryStrategyProvider cacheStrategy)
        : base(memoryCache, cacheStrategy)
    {
        _dataProvider = dataProvider;
    }

    protected override IEnumerable<object?> Fetch(ICacheEntry cacheEntry, NodeDataLoaderKey key)
    {
        var result = _dataProvider.GetNodes<TNode, TNodeDto>(key.ParentPath, key.CommitId.Sha, key.IsRecursive);

        if (key.Id is not null)
        {
            result = result.Where(x => x.Id == key.Id);
        }

        return result.ToList();
    }
}

internal record NodeDataLoaderKey
{
    internal NodeDataLoaderKey(IResolveFieldContext context)
    {
        ParentNode = context.Source is NodeDto dto ? dto.Node : null;
        ParentPath = ParentNode?.Path?.FilePath ?? context.GetArgument(GitObjectDbQuery.ParentPathArgument, default(string?));
        CommitId = context.GetCommitId();
        IsRecursive = context.GetArgumentFromParentContexts(GitObjectDbQuery.IsRecursiveArgument, false);
        Id = context.GetArgument(GitObjectDbQuery.IdArgument, default(string?));
    }

    public Node? ParentNode { get; }

    public string? ParentPath { get; }

    public ObjectId CommitId { get; }

    public bool IsRecursive { get; }

    public string? Id { get; }
}