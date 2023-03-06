using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using System.IO;

namespace GitObjectDb.Internal.Queries;

internal class LoadItem : IQuery<LoadItem.Parameters, TreeItem>
{
    public TreeItem Execute(IQueryAccessor queryAccessor, Parameters parms)
    {
        var loadParameters = new DataLoadParameters(parms.Path, parms.Tree.Id);
        return queryAccessor.Cache.GetOrCreate(loadParameters, Load) ??
               Load(default);

        TreeItem Load(ICacheEntry? entry) =>
            parms.Path.IsNode ?
            LoadNode(queryAccessor, parms) :
            LoadResource(parms);
    }

    private TreeItem LoadNode(IQueryAccessor queryAccessor, Parameters parms)
    {
        using var stream = GetStream(parms);
        return queryAccessor.Serializer.Deserialize(stream,
                                                    parms.Tree.Id,
                                                    parms.Path,
                                                    p => Execute(queryAccessor, parms with { Path = p }));
    }

    private static TreeItem LoadResource(Parameters parms) =>
        new Resource(parms.Path, new Resource.Data(() => GetStream(parms)));

    private static Stream GetStream(Parameters parms)
    {
        var filePath = parms.Path.FilePath;
        var treeEntry = parms.Tree[filePath];

        if (treeEntry is null)
        {
            throw new GitObjectDbException($"The entry at the path {filePath} does not exist.");
        }
        var blob = treeEntry.Target.Peel<Blob>();
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
