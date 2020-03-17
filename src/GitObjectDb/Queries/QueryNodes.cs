using GitObjectDb.Serialization.Json;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Queries
{
    internal class QueryNodes : IQuery<DataPath, string, Node>, IQuery<Node, string, IEnumerable<Node>>, IQuery<Tree, Stack<string>, IEnumerable<Node>>
    {
        private readonly IQuery<Tree, DataPath, ITreeItem> _loader;

        public QueryNodes(IQuery<Tree, DataPath, ITreeItem> loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        public Node Execute(Repository repository, DataPath path, string committish = null)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var commit = committish != null ?
                (Commit)repository.Lookup(committish) :
                repository.Head.Tip;
            return _loader.Execute(repository, commit.Tree[path.FolderPath].Target.Peel<Tree>(), path) as Node;
        }

        public IEnumerable<Node> Execute(Repository repository, Node node = null, string committish = null)
        {
            if (node != null && node.Path == null)
            {
                throw new ArgumentNullException("node.Path");
            }

            var commit = committish != null ?
                (Commit)repository.Lookup(committish) :
                repository.Head.Tip;
            var tree = node == null || string.IsNullOrEmpty(node.Path.FolderPath) ?
                commit.Tree :
                commit.Tree[node.Path.FolderPath].Target.Peel<Tree>();
            return Execute(repository, tree, DataPath.ToStack(node?.Path));
        }

        public IEnumerable<Node> Execute(Repository repository, Tree tree, Stack<string> stack)
        {
            foreach (var folderChildTree in tree.Where(e => e.TargetType == TreeEntryTargetType.Tree))
            {
                stack.Push(folderChildTree.Name);
                foreach (var childFolder in folderChildTree.Target.Peel<Tree>().Where(e => e.TargetType == TreeEntryTargetType.Tree))
                {
                    stack.Push(childFolder.Name);
                    if (UniqueId.TryParse(childFolder.Name, out _))
                    {
                        var nestedTree = childFolder.Target.Peel<Tree>();
                        if (nestedTree.Any(e => e.Name == FileSystemStorage.DataFile))
                        {
                            var path = DataPath.FromStack(stack, FileSystemStorage.DataFile);
                            yield return (Node)_loader.Execute(repository, nestedTree, path);
                        }
                    }
                    stack.Pop();
                }
                stack.Pop();
            }
        }
    }
}
