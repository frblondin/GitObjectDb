using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using System.IO;

namespace GitObjectDb.Internal.Queries;

internal class LoadItem : IQuery<LoadItem.Parameters, ITreeItem>
{
    public ITreeItem Execute(IQueryAccessor queryAccessor, Parameters parms)
    {
        var loadParameters = new DataLoadParameters(parms.Path, parms.Tree.Id);
        return queryAccessor.ReferenceCache?.GetOrCreate(loadParameters, Load) ??
               Load(default);

        ITreeItem Load(ICacheEntry? entry) =>
            parms.Path.IsNode ?
            LoadNode(queryAccessor, parms) :
            LoadResource(parms);
    }

    private ITreeItem LoadNode(IQueryAccessor queryAccessor, Parameters parms)
    {
        using var stream = GetStream(parms);
        return queryAccessor.Serializer.Deserialize(stream,
                                       parms.Tree.Id,
                                       parms.Path,
                                       p => Execute(queryAccessor, parms with { Path = p }));
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
        public Parameters(Tree tree, DataPath path)
        {
            Tree = tree;
            Path = path;
        }

        public Tree Tree { get; }

        public DataPath Path { get; init; }
    }
}
