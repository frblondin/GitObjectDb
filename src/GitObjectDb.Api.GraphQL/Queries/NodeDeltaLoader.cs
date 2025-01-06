using GitObjectDb.Api.GraphQL.Graph;
using GitObjectDb.Api.GraphQL.Model;
using GitObjectDb.Api.GraphQL.Tools;
using GraphQL;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GitObjectDb.Api.GraphQL.Queries;

internal class NodeDeltaLoader<TNode>(IQueryAccessor queryAccessor,
                                     IMemoryCache memoryCache,
                                     IOptions<GitObjectDbGraphQLOptions> options)
    : CachedResultLoaderBase<NodeDeltaDataLoaderKey, IEnumerable<DeltaDto>>(memoryCache, options)
    where TNode : Node
{
    protected override IEnumerable<DeltaDto> Fetch(ICacheEntry cacheEntry, NodeDeltaDataLoaderKey key)
    {
        var connection = queryAccessor as IConnection ??
            throw new GitObjectDbException("Delta can only be retrieved when the query accessor is a GitObjectDb connection.");
        var changes = connection.Compare(key.Start.Sha, key.End.Sha);
        var result = from change in changes
                     where change.New is TNode || change.Old is TNode
                     select new DeltaDto<TNode>((TNode?)change.Old, (TNode?)change.New, changes.End.Id, change.New is null);
        return [ ..result];
    }
}

internal record NodeDeltaDataLoaderKey
{
    public NodeDeltaDataLoaderKey(IResolveFieldContext context)
    {
        Start = context.GetCommitId(Query.DeltaStartCommit);
        End = context.GetCommitId(Query.DeltaEndCommit);
    }

    public ObjectId Start { get; }

    public ObjectId End { get; }
}