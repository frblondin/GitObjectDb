using GitObjectDb.Serialization.Json;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Queries
{
    internal class QueryResources : IQuery<DataPath, Tree, IEnumerable<Resource>>
    {
        private readonly IQuery<Tree, DataPath, ITreeItem> _loader;

        public QueryResources(IQuery<Tree, DataPath, ITreeItem> loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        public IEnumerable<Resource> Execute(Repository repository, DataPath path, Tree tree)
        {
            var referenceResourceTree = tree[FileSystemStorage.ResourceFolder];
            if (referenceResourceTree?.TargetType == TreeEntryTargetType.Tree)
            {
                var traversed = referenceResourceTree.Traverse($"{path.FolderPath}/{FileSystemStorage.ResourceFolder}");
                foreach (var entry in traversed)
                {
                    if (entry.Entry.TargetType == TreeEntryTargetType.Blob)
                    {
                        yield return new Resource(
                            DataPath.FromGitBlobPath(entry.Path),
                            entry.Entry.Target.Peel<Blob>());
                    }
                }
            }
        }
    }
}
