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
    /// <typeparam name="TNodeDto">The type of the data transfer object of the result collection.</typeparam>
    /// <param name="committish">The optional committish (head tip is used otherwise).</param>
    /// <param name="parentPath">The optional node parent path when retriving node children.</param>
    /// <param name="isRecursive">Gets whether all nested nodes should be returned.</param>
    /// <returns>The item being found, if any.</returns>
    public IEnumerable<TNodeDto> GetNodes<TNode, TNodeDto>(string committish,
                                                           string? parentPath = null,
                                                           bool isRecursive = false)
        where TNode : Node
        where TNodeDto : NodeDto
    {
        var parent = parentPath != null ?
            QueryAccessor.Lookup<Node>(committish, DataPath.Parse(parentPath)) :
            null;
        var result = QueryAccessor.GetNodes<TNode>(committish, parent, isRecursive);

        return MapItemsCached<TNode, TNodeDto>((IEnumerable<TNode>?)result, result.CommitId)!;
    }

    /// <summary>Executes a mapping from the source to a new destination object.</summary>
    /// <typeparam name="TNode">Source type to use.</typeparam>
    /// <typeparam name="TNodeDto">Destination type to create.</typeparam>
    /// <param name="source">Source object to map from.</param>
    /// <param name="commitId">Commit containing the object.</param>
    /// <returns>Mapped destination object.</returns>
    public TNodeDto? MapCached<TNode, TNodeDto>(TNode? source, ObjectId commitId)
        where TNode : Node
        where TNodeDto : NodeDto
    {
        if (source is null)
        {
            return default;
        }

        return _cache.GetOrCreate(CreateCacheKey(source, commitId), cacheEntry =>
        {
            UpdateExpiration(cacheEntry);

            return Map<TNode, TNodeDto>(source, commitId);
        });
    }

    private static object CreateCacheKey<TNode>(TNode source, ObjectId commitId)
        where TNode : Node
    {
        return (source.Path!, commitId);
    }

    private TNodeDto? Map<TNode, TNodeDto>(TNode? source, ObjectId commitId)
        where TNode : Node
        where TNodeDto : NodeDto
    {
        if (source is null)
        {
            return default;
        }

#pragma warning disable CS8974 // Converting method group to non-delegate type
        return _mapper.Map<TNode, TNodeDto>(
            source,
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
    /// <typeparam name="TNodeDto">Destination type to create.</typeparam>
    /// <param name="source">Source object to map from.</param>
    /// <param name="commitId">Commit containing the object.</param>
    /// <returns>Mapped destination object.</returns>
    public IEnumerable<TNodeDto>? MapItemsCached<TNode, TNodeDto>(IEnumerable<TNode>? source, ObjectId commitId)
        where TNode : Node
        where TNodeDto : NodeDto
    {
        return source?.Select(i => Map<TNode, TNodeDto>(i, commitId)!);
    }

    private static void UpdateExpiration(ICacheEntry entry)
    {
        // TODO: make it configurable
        entry.SetSlidingExpiration(TimeSpan.FromMinutes(1));
    }
}
