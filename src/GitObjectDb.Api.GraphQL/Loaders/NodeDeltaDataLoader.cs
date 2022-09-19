using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Api.GraphQL.Tools;
using GitObjectDb.Api.Model;
using GraphQL;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL.Loaders;

internal class NodeDeltaDataLoader<TNode, TNodeDto> : GitObjectDbDataLoaderBase<NodeDeltaDataLoaderKey, IEnumerable<object?>>
    where TNode : Node
    where TNodeDto : NodeDto
{
    private readonly DataProvider _dataProvider;

    public NodeDeltaDataLoader(DataProvider dataProvider, IMemoryCache memoryCache, CacheEntryStrategyProvider cacheStrategy)
        : base(memoryCache, cacheStrategy)
    {
        _dataProvider = dataProvider;
    }

    protected override IEnumerable<object?> Fetch(ICacheEntry cacheEntry, NodeDeltaDataLoaderKey key)
    {
        return _dataProvider.GetNodeDeltas<TNode, TNodeDto>(key.Start, key.End);
    }
}

internal record NodeDeltaDataLoaderKey
{
    internal NodeDeltaDataLoaderKey(IResolveFieldContext context)
    {
        Start = context.GetCommitId(GitObjectDbQuery.DeltaStartCommit);
        End = context.GetCommitId(GitObjectDbQuery.DeltaEndCommit);
    }

    public ObjectId Start { get; }

    public ObjectId End { get; }
}