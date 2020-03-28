using GitObjectDb.Serialization;
using LibGit2Sharp;
using System.Collections.Generic;

namespace GitObjectDb.Internal.Queries
{
    internal class LoadItem : IQuery<LoadItem.Parameters, ITreeItem>
    {
        private readonly NodeSerializerCache _serializer;

        public LoadItem(NodeSerializerCache serializer)
        {
            _serializer = serializer;
        }

        public ITreeItem Execute(IConnectionInternal connection, Parameters parms)
        {
            if (parms.ReferenceCache == null || !parms.ReferenceCache.TryGetValue(parms.Path, out var result))
            {
                result = parms.Path.FileName == FileSystemStorage.DataFile ?
                    LoadNode(parms) :
                    LoadResource(parms);
                if (parms.ReferenceCache != null)
                {
                    parms.ReferenceCache[parms.Path] = result;
                }
            }
            return result;
        }

        private ITreeItem LoadNode(Parameters parms)
        {
            var blob = parms.Tree[parms.Path.FilePath].Target.Peel<Blob>();
            return _serializer.Deserialize(blob.GetContentStream(),
                                           parms.Path,
                                           blob.Id.Sha,
                                           p => LoadNode(parms with { Path = p })).Node;
        }

        private static ITreeItem LoadResource(Parameters parms)
        {
            var blob = parms.Tree[parms.Path.FilePath].Target.Peel<Blob>();
            return new Resource(parms.Path, blob);
        }

        internal record Parameters
        {
            public Parameters(Tree tree, DataPath path, IDictionary<DataPath, ITreeItem>? referenceCache)
            {
                Tree = tree;
                Path = path;
                ReferenceCache = referenceCache;
            }

            public Tree Tree { get; }

            public DataPath Path { get; set; }

            public IDictionary<DataPath, ITreeItem>? ReferenceCache { get; }
        }
    }
}
