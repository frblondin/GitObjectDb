using GitObjectDb.Serialization;
using LibGit2Sharp;
using System.Collections.Generic;

namespace GitObjectDb.Internal.Queries
{
    internal class LoadItem : IQuery<LoadItem.Parameters, ITreeItem>
    {
        private readonly INodeSerializer _serializer;

        public LoadItem(INodeSerializer serializer)
        {
            _serializer = serializer;
        }

        public ITreeItem Execute(IConnectionInternal connection, Parameters parms)
        {
            if (parms.ReferenceCache == null || !parms.ReferenceCache.TryGetValue(parms.Path, out var result))
            {
                result = parms.Path.IsNode ?
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
                                           p => LoadNode(parms with { Path = p }));
        }

        private static ITreeItem LoadResource(Parameters parms)
        {
            return new Resource(parms.Path,
                                new Resource.Data(() => parms.Tree[parms.Path.FilePath].Target.Peel<Blob>().GetContentStream()));
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
