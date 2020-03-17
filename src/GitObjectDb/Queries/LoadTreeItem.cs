using GitObjectDb.Serialization.Json;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Queries
{
    internal class LoadTreeItem : IQuery<Tree, DataPath, ITreeItem>
    {
        private readonly INodeSerializer _serializer;

        public LoadTreeItem(INodeSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public ITreeItem Execute(Repository repository, Tree tree, DataPath path)
        {
            if (tree is null)
            {
                throw new ArgumentNullException(nameof(tree));
            }

            return path.FileName == FileSystemStorage.DataFile ?
                LoadNode(tree, path) :
                LoadResource(tree, path);
        }

        private ITreeItem LoadNode(Tree tree, DataPath path)
        {
            var blob = tree[path.FileName].Target.Peel<Blob>();
            var result = _serializer.Deserialize(blob.GetContentStream(), path).Node;

            LoadResources(tree, result);
            return result;
        }

        private static ITreeItem LoadResource(Tree tree, DataPath path) =>
            new Resource(path, tree[path.FileName].Target.Peel<Blob>());

        private static void LoadResources(Tree tree, Node node)
        {
            var referenceResourceTree = tree[FileSystemStorage.ResourceFolder];
            if (referenceResourceTree?.TargetType == TreeEntryTargetType.Tree)
            {
                var traversed = referenceResourceTree.Traverse(string.Empty);
                foreach (var entry in traversed)
                {
                    if (entry.Entry.TargetType == TreeEntryTargetType.Blob)
                    {
                        node.Resources.Add(
                            DataPath.FromGitBlobPath(entry.Path),
                            entry.Entry.Target.Peel<Blob>());
                    }
                }
            }
            node.Resources.IsDetached = false;
        }
    }
}
