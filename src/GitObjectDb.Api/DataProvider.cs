using AutoMapper;
using GitObjectDb.Api.Model;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace GitObjectDb.Api;

public sealed class DataProvider
{
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    public DataProvider(IConnection connection, IMapper mapper, IMemoryCache cache)
    {
        Connection = connection;
        _mapper = mapper;
        _cache = cache;
    }

    public IConnection Connection { get; }

    public IEnumerable<TNodeDTO> GetNodes<TNode, TNodeDTO>(string? parentPath = null, string? committish = null, bool isRecursive = false)
        where TNode : Node
        where TNodeDTO : NodeDTO
    {
        var commit = LookupCommit(committish);
        var referenceCache = _cache.GetOrCreate<ConcurrentDictionary<DataPath, ITreeItem>>(commit.Sha, _ => new());
        var parent = parentPath != null ?
            Connection.Lookup<Node>(
                DataPath.Parse(parentPath),
                commit.Sha) :
            null;
        var result = Connection.GetNodes<TNode>(parent, commit.Sha, isRecursive, referenceCache);
#pragma warning disable CS8974 // Converting method group to non-delegate type
        return _mapper.Map<IEnumerable<TNode>, IEnumerable<TNodeDTO>>(
            result,
            opt => opt.Items[AutoMapperProfile.ChildResolverName] = ResolveChildren);

        IEnumerable<Node> ResolveChildren(Node parent) =>
            Connection.GetNodes<Node>(parent, commit.Sha, isRecursive: false, referenceCache);
    }

    private Commit LookupCommit(string? committish) =>
        committish != null ?
        (Commit)Connection.Repository.Lookup(committish) :
        Connection.Repository.Head.Tip;
}
