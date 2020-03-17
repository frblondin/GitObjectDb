using GitObjectDb.Serialization.Json;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Queries
{
    internal class QueryNodes : IQuery<DataPath, Tree, IEnumerable<Node>>
    {
        private readonly IQuery<Tree, DataPath, ITreeItem> _loader;

        public QueryNodes(IQuery<Tree, DataPath, ITreeItem> loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        public IEnumerable<Node> Execute(Repository repository, DataPath path, Tree tree)
        {
            if (tree is null)
            {
                throw new ArgumentNullException(nameof(tree));
            }

            foreach (var folderChildTree in tree.Where(e => e.TargetType == TreeEntryTargetType.Tree))
            {
                foreach (var childFolder in folderChildTree.Target.Peel<Tree>().Where(e => e.TargetType == TreeEntryTargetType.Tree))
                {
                    if (UniqueId.TryParse(childFolder.Name, out var id))
                    {
                        var nestedTree = childFolder.Target.Peel<Tree>();
                        if (nestedTree.Any(e => e.Name == FileSystemStorage.DataFile))
                        {
                            var childPath =
                                path?.AddChild(folderChildTree.Name, id) ??
                                DataPath.Root(folderChildTree.Name, id);
                            yield return (Node)_loader.Execute(repository, nestedTree, childPath);
                        }
                    }
                }
            }
        }
    }
}
