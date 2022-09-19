using AutoMapper;
using GitObjectDb.Api.Model;
using Microsoft.Extensions.Caching.Memory;

namespace GitObjectDb.Api;

public sealed class DataProvider
{
    private readonly IMapper _mapper;

    public DataProvider(IQueryAccessor queryAccessor, IMapper mapper)
    {
        QueryAccessor = queryAccessor;
        _mapper = mapper;
    }

    public IQueryAccessor QueryAccessor { get; }

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

        return Map<IEnumerable<TNode>, IEnumerable<TNodeDto>>(result, committish);
    }

    public IEnumerable<DeltaDto<TNodeDto>> GetNodeDeltas<TNode, TNodeDto>(string startCommittish, string? endCommittish)
        where TNode : Node
        where TNodeDto : NodeDto
    {
        var connection = QueryAccessor as IConnection ??
            throw new GitObjectDbException("Delta can only be retrieved when the query accessor is a GitObjectDb connection.");
        var changes = connection.Compare(startCommittish, endCommittish);
        return from change in changes
               where change.New is TNode || change.Old is TNode
               let old = change.Old is not null ? Map<TNode, TNodeDto>((TNode)change.Old, changes.End.Sha) : null
               let @new = change.New is not null ? Map<TNode, TNodeDto>((TNode)change.New!, changes.End.Sha) : null
               select new DeltaDto<TNodeDto>(old, @new, changes.End, change.New is null);
    }

    private TDestination Map<TSource, TDestination>(TSource source, string? committish)
    {
#pragma warning disable CS8974 // Converting method group to non-delegate type
        return _mapper.Map<TSource, TDestination>(
            source,
            opt => opt.Items[AutoMapperProfile.ChildResolverName] = ResolveChildren);

        IEnumerable<Node> ResolveChildren(Node p) =>
            QueryAccessor.GetNodes<Node>(p, committish, isRecursive: false);
    }
}
