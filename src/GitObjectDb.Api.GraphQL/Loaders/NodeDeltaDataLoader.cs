using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Api.GraphQL.Model;
using GitObjectDb.Api.GraphQL.Tools;
using GraphQL;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;

namespace GitObjectDb.Api.GraphQL.Loaders;

internal class NodeDeltaDataLoader<TNode> : GitObjectDbDataLoaderBase<NodeDeltaDataLoaderKey, IEnumerable<object?>>
    where TNode : Node
{
    private readonly IQueryAccessor _queryAccessor;

    public NodeDeltaDataLoader(IQueryAccessor queryAccessor, IMemoryCache memoryCache, CacheEntryStrategyProvider cacheStrategy)
        : base(memoryCache, cacheStrategy)
    {
        _queryAccessor = queryAccessor;
    }

    protected override IEnumerable<object?> Fetch(ICacheEntry cacheEntry, NodeDeltaDataLoaderKey key)
    {
        var connection = _queryAccessor as IConnection ??
            throw new GitObjectDbException("Delta can only be retrieved when the query accessor is a GitObjectDb connection.");
        var changes = connection.Compare(key.Start.Sha, key.End.Sha);
        return from change in changes
               where change.New is TNode || change.Old is TNode
               select new DeltaDto<TNode>((TNode?)change.Old, (TNode?)change.New, changes.End.Id, change.New is null);
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