using AutoMapper;
using GitDotNet;
using GitObjectDb.Api.OData.Model;
using Microsoft.Extensions.Caching.Memory;

namespace GitObjectDb.Api.OData;

/// <summary>Returns data transfer objects from collection of items returned by <see cref="IQueryAccessor"/>.</summary>
/// <remarks>Initializes a new instance of the <see cref="DataProvider"/> class.</remarks>
/// <param name="connection">The query accessor.</param>
/// <param name="mapper">The <see cref="IMapper"/> used to project items to their data transfer type equivalent.</param>
/// <param name="cache">The cache to be used.</param>
public sealed class DataProvider(IConnection connection, IMapper mapper, IMemoryCache cache)
{
    /// <summary>
    /// Retrieves nodes of type <typeparamref name="TNode"/> and projects the result to
    /// the <typeparamref name="TNodeDto"/> type.
    /// </summary>
    /// <typeparam name="TNode">The type of node to be queried.</typeparam>
    /// <param name="description">The data transfer type description.</param>
    /// <param name="committish">The optional committish (head tip is used otherwise).</param>
    /// <param name="parentPath">The optional node parent path when retrieving node children.</param>
    /// <param name="isRecursive">Gets whether all nested nodes should be returned.</param>
    /// <returns>The item being found, if any.</returns>
    internal async Task<IEnumerable<NodeDto>> GetNodesAsync<TNode>(DataTransferTypeDescription description,
        string committish, string? parentPath = null, bool isRecursive = false)
        where TNode : Node
    {
        var commit = await connection.Repository.GetCommittishAsync(committish) ??
            throw new InvalidOperationException($"Commit {committish} could not be found.");
        return await GetNodesAsync<TNode>(description, commit, parentPath, isRecursive);
    }

    /// <summary>
    /// Retrieves nodes of type <typeparamref name="TNode"/> and projects the result to
    /// the <typeparamref name="TNodeDto"/> type.
    /// </summary>
    /// <typeparam name="TNode">The type of node to be queried.</typeparam>
    /// <param name="description">The data transfer type description.</param>
    /// <param name="commit">The commit to explore.</param>
    /// <param name="parentPath">The optional node parent path when retrieving node children.</param>
    /// <param name="isRecursive">Gets whether all nested nodes should be returned.</param>
    /// <returns>The item being found, if any.</returns>
    internal async Task<IEnumerable<NodeDto>> GetNodesAsync<TNode>(DataTransferTypeDescription description,
        CommitEntry commit, string? parentPath = null, bool isRecursive = false)
        where TNode : Node
    {
        var parent = parentPath != null ?
            await connection.LookupAsync<Node>(commit, DataPath.Parse(parentPath)) :
            null;
        var result = connection.GetNodesAsync<TNode>(commit, parent, isRecursive).ToEnumerable();

        return MapItemsCached((IEnumerable<TNode>?)result, description, commit)!;
    }

    /// <summary>Executes a mapping from the source to a new destination object.</summary>
    /// <typeparam name="TNode">Source type to use.</typeparam>
    /// <param name="source">Source object to map from.</param>
    /// <param name="description">The data transfer type description.</param>
    /// <param name="commit">Commit containing the object.</param>
    /// <returns>Mapped destination object.</returns>
    public NodeDto? MapCached<TNode>(TNode? source, DataTransferTypeDescription description, CommitEntry commit)
        where TNode : Node
    {
        if (source is null)
        {
            return default;
        }

        return cache.GetOrCreate(CreateCacheKey(source, commit.Id), cacheEntry =>
        {
            UpdateExpiration(cacheEntry);

            return Map(source, description, commit);
        });
    }

    private static object CreateCacheKey<TNode>(TNode source, HashId commitId)
        where TNode : Node
    {
        return (source.Path!, commitId);
    }

    private NodeDto? Map<TNode>(TNode? source, DataTransferTypeDescription description, CommitEntry commit)
        where TNode : Node
    {
        if (source is null)
        {
            return default;
        }

#pragma warning disable CS8974 // Converting method group to non-delegate type
        return (NodeDto?)mapper.Map(
            source,
            typeof(TNode),
            description.DtoType,
            opt =>
            {
                opt.Items[AutoMapperProfile.Commit] = commit;
                opt.Items[AutoMapperProfile.ChildResolver] = ResolveChildren;
            });

        IEnumerable<Node> ResolveChildren(Node p) =>
            connection.GetNodesAsync<Node>(commit, p, isRecursive: false).ToEnumerable();
    }

    /// <summary>Executes a mapping from the source to a new destination object.</summary>
    /// <typeparam name="TNode">Source type to use.</typeparam>
    /// <param name="source">Source object to map from.</param>
    /// <param name="description">The data transfer type description.</param>
    /// <param name="commit">Commit containing the object.</param>
    /// <returns>Mapped destination object.</returns>
    public IEnumerable<NodeDto>? MapItemsCached<TNode>(IEnumerable<TNode>? source, DataTransferTypeDescription description, CommitEntry commit)
        where TNode : Node
    {
        return source?.Select(i => MapCached(i, description, commit)!);
    }

    private static void UpdateExpiration(ICacheEntry entry)
    {
        // TODO: make it configurable
        entry.SetSlidingExpiration(TimeSpan.FromMinutes(1));
    }
}
