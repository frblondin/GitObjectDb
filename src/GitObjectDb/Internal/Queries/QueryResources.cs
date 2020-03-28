using LibGit2Sharp;
using System.Collections.Generic;

namespace GitObjectDb.Internal.Queries
{
    internal class QueryResources : IQuery<DataPath, Tree, IEnumerable<Resource>>
    {
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
