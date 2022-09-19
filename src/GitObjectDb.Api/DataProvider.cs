using AutoMapper;
using GitObjectDb.Api.Model;
using LibGit2Sharp;

namespace GitObjectDb.Api;

/// <summary>
/// Returns data transfer objects from collection of items returned by <see cref="IQueryAccessor"/>.
/// </summary>
public sealed class DataProvider
{
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataProvider"/> class.
    /// </summary>
    /// <param name="queryAccessor">The query accessor.</param>
    /// <param name="mapper">
    /// The <see cref="IMapper"/> used to project items to their data transfer type equivalent.
    /// </param>
    public DataProvider(IQueryAccessor queryAccessor, IMapper mapper)
    {
        QueryAccessor = queryAccessor;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets the query accessor used to access GitObjectDb items.
    /// </summary>
    public IQueryAccessor QueryAccessor { get; }

    /// <summary>
    /// Retrieves nodes of type <typeparamref name="TNode"/> and projects the result to
    /// the <typeparamref name="TNodeDto"/> type.
    /// </summary>
    /// <typeparam name="TNode">The type of node to be queried.</typeparam>
    /// <typeparam name="TNodeDto">The type of the data transfer object of the result collection.</typeparam>
    /// <param name="parentPath">The optional node parent path when retriving node children.</param>
    /// <param name="committish">The optional committish (head tip is used otherwise).</param>
    /// <param name="isRecursive">Gets whether all nested nodes should be returned.</param>
    /// <returns>The item being found, if any.</returns>
    public IEnumerable<TNodeDto> GetNodes<TNode, TNodeDto>(string? parentPath = null,
                                                           string? committish = null,
                                                           bool isRecursive = false)
        where TNode : Node
        where TNodeDto : NodeDto
    {
        var parent = parentPath != null ?
            QueryAccessor.Lookup<Node>(DataPath.Parse(parentPath), committish) :
            null;
        var result = QueryAccessor.GetNodes<TNode>(parent, committish, isRecursive);

        return Map<IEnumerable<TNode>, IEnumerable<TNodeDto>>(result, result.CommitId)!;
    }

    /// <summary>
    /// Returns all changes that occurred between <paramref name="startCommittish"/> and
    /// <paramref name="endCommittish"/>.
    /// </summary>
    /// <typeparam name="TNode">The type of node to be queried.</typeparam>
    /// <typeparam name="TNodeDto">The type of the data transfer object of the result collection.</typeparam>
    /// <param name="startCommittish">The start commit of comparison.</param>
    /// <param name="endCommittish">The optional end commit of comparison.</param>
    /// <returns>The item being found, if any.</returns>
    public IEnumerable<DeltaDto<TNodeDto>> GetNodeDeltas<TNode, TNodeDto>(string startCommittish, string? endCommittish)
        where TNode : Node
        where TNodeDto : NodeDto
    {
        var connection = QueryAccessor as IConnection ??
            throw new GitObjectDbException("Delta can only be retrieved when the query accessor is a GitObjectDb connection.");
        var changes = connection.Compare(startCommittish, endCommittish);
        return from change in changes
               where change.New is TNode || change.Old is TNode
               let old = Map<TNode, TNodeDto>((TNode?)change.Old, changes.Start.Id)
               let @new = Map<TNode, TNodeDto>((TNode?)change.New!, changes.End.Id)
               select new DeltaDto<TNodeDto>(old, @new, changes.End.Id, change.New is null);
    }

    /// <summary>
    /// Executes a mapping from the source to a new destination object.
    /// </summary>
    /// <typeparam name="TSource">Source type to use.</typeparam>
    /// <typeparam name="TDestination">Destination type to create.</typeparam>
    /// <param name="source">Source object to map from.</param>
    /// <param name="commitId">Commit containing the object.</param>
    /// <returns>Mapped destination object.</returns>
    public TDestination? Map<TSource, TDestination>(TSource? source, ObjectId commitId)
    {
        if (source is null)
        {
            return default;
        }

#pragma warning disable CS8974 // Converting method group to non-delegate type
        return _mapper.Map<TSource, TDestination>(
            source,
            opt =>
            {
                opt.Items[AutoMapperProfile.CommitId] = commitId;
                opt.Items[AutoMapperProfile.ChildResolver] = ResolveChildren;
            });

        IEnumerable<Node> ResolveChildren(Node p) =>
            QueryAccessor.GetNodes<Node>(p, commitId.Sha, isRecursive: false);
    }
}
