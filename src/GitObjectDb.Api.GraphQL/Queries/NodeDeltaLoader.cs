using GitDotNet;
using GitObjectDb.Api.GraphQL.Graph;
using GitObjectDb.Api.GraphQL.Model;
using GitObjectDb.Api.GraphQL.Tools;
using GraphQL;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GitObjectDb.Api.GraphQL.Queries;

internal class NodeDeltaLoader<TNode>(IConnection queryAccessor,
                                     IMemoryCache memoryCache,
                                     IOptions<GitObjectDbGraphQLOptions> options)
    : CachedResultLoaderBase<NodeDeltaDataLoaderKey, IEnumerable<DeltaDto>>(memoryCache, options)
    where TNode : Node
{
    protected override async Task<IEnumerable<DeltaDto>> FetchAsync(ICacheEntry cacheEntry, NodeDeltaDataLoaderKey key)
    {
        var startId = await key.GetStartAsync();
        var endId = await key.GetEndAsync();
        var changes = await queryAccessor.CompareAsync(startId.ToString(), endId.ToString());
        var result = from change in changes
                     where change.New is TNode || change.Old is TNode
                     select new DeltaDto<TNode>((TNode?)change.Old, (TNode?)change.New, changes.End.Id, change.New is null);
        return [ ..result];
    }
}

internal record NodeDeltaDataLoaderKey(IResolveFieldContext Context)
{
    public async Task<HashId> GetStartAsync() => await Context.GetCommitIdAsync(Query.DeltaStartCommit);

    public async Task<HashId> GetEndAsync() => await Context.GetCommitIdAsync(Query.DeltaEndCommit);
}