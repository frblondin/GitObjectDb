using GitObjectDb.Serialization;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using System.IO;

namespace GitObjectDb.Internal.Queries;

internal class LoadItem : IQuery<LoadItem.Parameters, ITreeItem>
{
    private readonly INodeSerializer _serializer;

    public LoadItem(INodeSerializer serializer)
    {
        _serializer = serializer;
    }

    public ITreeItem Execute(IQueryAccessor queryAccessor, Parameters parms)
    {
        var loadParameters = new DataLoadParameters(parms.Path, parms.Tree.Id);
        if (parms.ReferenceCache is not null)
        {
            return parms.ReferenceCache.GetOrCreate(loadParameters,
                                                    _ => Load(loadParameters));
        }
        else
        {
            return Load(loadParameters);
        }

        ITreeItem Load(DataLoadParameters p) =>
            parms.Path.IsNode ?
            LoadNode(queryAccessor, parms) :
            LoadResource(parms);
    }

    private ITreeItem LoadNode(IQueryAccessor queryAccessor, Parameters parms)
    {
        using var stream = GetStream(parms);
        return _serializer.Deserialize(stream,
                                       parms.Tree.Id,
                                       parms.Path,
                                       queryAccessor.Model,
                                       p => LoadNode(queryAccessor, parms with { Path = p }));
    }

    private static ITreeItem LoadResource(Parameters parms) =>
        new Resource(parms.Path, new Resource.Data(() => GetStream(parms)));

    private static Stream GetStream(Parameters parms)
    {
        var blob = parms.Tree[parms.Path.FilePath].Target.Peel<Blob>();
        return blob.GetContentStream();
    }

    internal record Parameters
    {
        public Parameters(Tree tree, DataPath path, IMemoryCache referenceCache)
        {
            Tree = tree;
            Path = path;
            ReferenceCache = referenceCache;
        }

        public Tree Tree { get; }

        public DataPath Path { get; init; }

        public IMemoryCache ReferenceCache { get; }
    }
}
