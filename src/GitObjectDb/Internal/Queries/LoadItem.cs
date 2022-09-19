using GitObjectDb.Serialization;
using LibGit2Sharp;
using System.Collections.Concurrent;
using System.IO;

namespace GitObjectDb.Internal.Queries;

internal class LoadItem : IQuery<LoadItem.Parameters, ITreeItem>
{
    private readonly INodeSerializer _serializer;

    public LoadItem(INodeSerializer serializer)
    {
        _serializer = serializer;
    }

    public ITreeItem Execute(IConnection connection, Parameters parms)
    {
        return parms.ReferenceCache?.GetOrAdd(parms.Path, Load) ?? Load(parms.Path);

        ITreeItem Load(DataPath path) =>
            parms.Path.IsNode ?
            LoadNode(connection, parms) :
            LoadResource(parms);
    }

    private ITreeItem LoadNode(IConnection connection, Parameters parms)
    {
        using var stream = GetStream(parms);
        return _serializer.Deserialize(stream,
                                       parms.Path,
                                       connection.Model,
                                       p => LoadNode(connection, parms with { Path = p }));
    }

    private static ITreeItem LoadResource(Parameters parms) =>
        new Resource(parms.Path, new Resource.Data(() => GetStream(parms)));

    private static Stream GetStream(Parameters parms) =>
        parms.Tree[parms.Path.FilePath].Target.Peel<Blob>().GetContentStream();

    internal record Parameters
    {
        public Parameters(Tree tree, DataPath path, ConcurrentDictionary<DataPath, ITreeItem>? referenceCache)
        {
            Tree = tree;
            Path = path;
            ReferenceCache = referenceCache;
        }

        public Tree Tree { get; }

        public DataPath Path { get; init; }

        public ConcurrentDictionary<DataPath, ITreeItem>? ReferenceCache { get; }
    }
}
