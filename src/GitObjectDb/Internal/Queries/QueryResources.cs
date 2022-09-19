using LibGit2Sharp;
using System;
using System.Collections.Generic;

namespace GitObjectDb.Internal.Queries
{
    internal class QueryResources : IQuery<QueryResources.Parameters, IEnumerable<(DataPath Path, Lazy<Resource> Resource)>>
    {
        public IEnumerable<(DataPath Path, Lazy<Resource> Resource)> Execute(IConnectionInternal connection, Parameters parms)
        {
            var referenceResourceTree = parms.RelativeTree[FileSystemStorage.ResourceFolder];
            if (referenceResourceTree?.TargetType == TreeEntryTargetType.Tree)
            {
                var traversed = referenceResourceTree.Traverse($"{parms.Path.FolderPath}/{FileSystemStorage.ResourceFolder}");
                foreach (var entry in traversed)
                {
                    if (entry.Entry.TargetType == TreeEntryTargetType.Blob)
                    {
                        var path = DataPath.FromGitBlobPath(entry.Path);
                        yield return
                        (
                            path,
                            new Lazy<Resource>(() => new Resource(
                                path,
                                entry.Entry.Target.Peel<Blob>()))
                        );
                    }
                }
            }
        }

        internal class Parameters
        {
            public Parameters(Tree relativeTree, DataPath path)
            {
                RelativeTree = relativeTree;
                Path = path;
            }

            public Tree RelativeTree { get; }

            public DataPath Path { get; }
        }
    }
}
