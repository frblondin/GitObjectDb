using AutoMapper;
using GitObjectDb.Api.OData.Model;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;

namespace GitObjectDb.Api.OData;

/// <summary>Returns data transfer objects from collection of items returned by <see cref="IQueryAccessor"/>.</summary>
public sealed class DataProvider
{
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    /// <summary>Initializes a new instance of the <see cref="DataProvider"/> class.</summary>
    /// <param name="queryAccessor">The query accessor.</param>
    /// <param name="mapper">The <see cref="IMapper"/> used to project items to their data transfer type equivalent.</param>
    /// <param name="cache">The cache to be used.</param>
    public DataProvider(IQueryAccessor queryAccessor, IMapper mapper, IMemoryCache cache)
    {
        QueryAccessor = queryAccessor;
        _mapper = mapper;
        _cache = cache;
    }

    /// <summary>Gets the query accessor used to access GitObjectDb items.</summary>
    public IQueryAccessor QueryAccessor { get; }

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
    internal IEnumerable<NodeDto> GetNodes<TNode>(DataTransferTypeDescription description,
                                                string committish,
                                                string? parentPath = null,
                                                bool isRecursive = false)
        where TNode : Node
    {
        var parent = parentPath != null ?
            QueryAccessor.Lookup<Node>(committish, DataPath.Parse(parentPath)) :
            null;
        var result = QueryAccessor.GetNodes<TNode>(committish, parent, isRecursive);

        return MapItemsCached((IEnumerable<TNode>?)result, description, result.CommitId)!;
    }

    /// <summary>Executes a mapping from the source to a new destination object.</summary>
    /// <typeparam name="TNode">Source type to use.</typeparam>
    /// <param name="source">Source object to map from.</param>
    /// <param name="description">The data transfer type description.</param>
    /// <param name="commitId">Commit containing the object.</param>
    /// <returns>Mapped destination object.</returns>
    public NodeDto? MapCached<TNode>(TNode? source, DataTransferTypeDescription description, ObjectId commitId)
        where TNode : Node
    {
        if (source is null)
        {
            return default;
        }

        return _cache.GetOrCreate(CreateCacheKey(source, commitId), cacheEntry =>
        {
            UpdateExpiration(cacheEntry);

            return Map(source, description, commitId);
        });
    }

    private static object CreateCacheKey<TNode>(TNode source, ObjectId commitId)
        where TNode : Node
    {
        return (source.Path!, commitId);
    }

    private NodeDto? Map<TNode>(TNode? source, DataTransferTypeDescription description, ObjectId commitId)
        where TNode : Node
    {
        if (source is null)
        {
            return default;
        }

#pragma warning disable CS8974 // Converting method group to non-delegate type
        return (NodeDto?)_mapper.Map(
            source,
            typeof(TNode),
            description.DtoType,
            opt =>
            {
                opt.Items[AutoMapperProfile.CommitId] = commitId;
                opt.Items[AutoMapperProfile.ChildResolver] = ResolveChildren;
            });

        IEnumerable<Node> ResolveChildren(Node p) =>
            QueryAccessor.GetNodes<Node>(commitId.Sha, p, isRecursive: false);
    }

    /// <summary>Executes a mapping from the source to a new destination object.</summary>
    /// <typeparam name="TNode">Source type to use.</typeparam>
    /// <param name="source">Source object to map from.</param>
    /// <param name="description">The data transfer type description.</param>
    /// <param name="commitId">Commit containing the object.</param>
    /// <returns>Mapped destination object.</returns>
    public IEnumerable<NodeDto>? MapItemsCached<TNode>(IEnumerable<TNode>? source, DataTransferTypeDescription description, ObjectId commitId)
        where TNode : Node
    {
        return source?.Select(i => MapCached(i, description, commitId)!);
    }

    private static void UpdateExpiration(ICacheEntry entry)
    {
        // TODO: make it configurable
        entry.SetSlidingExpiration(TimeSpan.FromMinutes(1));
    }
}
