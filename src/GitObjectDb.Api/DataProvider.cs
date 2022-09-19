using AutoMapper;
using GitObjectDb.Api.Model;
using Microsoft.Extensions.Caching.Memory;

namespace GitObjectDb.Api;

public sealed class DataProvider
{
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    public DataProvider(IQueryAccessor queryAccessor, IMapper mapper, IMemoryCache cache)
    {
        QueryAccessor = queryAccessor;
        _mapper = mapper;
        _cache = cache;
    }

    public IQueryAccessor QueryAccessor { get; }

    public IEnumerable<TNodeDto> GetNodes<TNode, TNodeDto>(string? parentPath = null,
                                                           string? committish = null,
                                                           bool isRecursive = false)
        where TNode : Node
        where TNodeDto : NodeDto
    {
        var parent = parentPath != null ?
            QueryAccessor.Lookup<Node>(
                DataPath.Parse(parentPath),
                committish,
                _cache) :
            null;
        var result = QueryAccessor.GetNodes<TNode>(parent, committish, isRecursive, _cache);
#pragma warning disable CS8974 // Converting method group to non-delegate type
        return _mapper.Map<IEnumerable<TNode>, IEnumerable<TNodeDto>>(
            result,
            opt => opt.Items[AutoMapperProfile.ChildResolverName] = ResolveChildren);

        IEnumerable<Node> ResolveChildren(Node p) =>
            QueryAccessor.GetNodes<Node>(p, committish, isRecursive: false, _cache);
    }
}
