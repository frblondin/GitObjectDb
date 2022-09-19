using LibGit2Sharp;
using System;
using System.Collections.Generic;

namespace GitObjectDb.Internal.Queries
{
    internal class QueryResources : IQuery<QueryResources.Parameters, IEnumerable<(DataPath Path, Lazy<Resource> Resource)>>
    {
        private readonly IQuery<LoadItem.Parameters, ITreeItem> _loader;

        public QueryResources(IQuery<LoadItem.Parameters, ITreeItem> loader)
        {
            _loader = loader;
        }

        public IEnumerable<(DataPath Path, Lazy<Resource> Resource)> Execute(IConnectionInternal connection, Parameters parms)
        {
            var referenceResourceTree = parms.RelativeTree[FileSystemStorage.ResourceFolder];
            if (referenceResourceTree?.TargetType == TreeEntryTargetType.Tree)
            {
                var traversed = referenceResourceTree.Traverse($"{parms.Path.FolderPath}/{FileSystemStorage.ResourceFolder}");
                foreach (var (entry, path) in traversed)
                {
                    if (entry.TargetType == TreeEntryTargetType.Blob)
                    {
                        var dataPath = DataPath.Parse(path);
                        yield return
                        (
                            dataPath,
                            new Lazy<Resource>(() => (Resource)_loader.Execute(connection, new LoadItem.Parameters(parms.Tree, dataPath, parms.ReferenceCache)))
                        );
                    }
                }
            }
        }

        internal class Parameters
        {
            public Parameters(Tree tree, Tree relativeTree, DataPath path, IDictionary<DataPath, ITreeItem>? referenceCache)
            {
                Tree = tree;
                RelativeTree = relativeTree;
                Path = path;
                ReferenceCache = referenceCache;
            }

            public Tree Tree { get; }

            public Tree RelativeTree { get; }

            public DataPath Path { get; }

            public IDictionary<DataPath, ITreeItem>? ReferenceCache { get; }
        }
    }
}
