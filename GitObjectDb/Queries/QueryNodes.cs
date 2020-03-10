using GitObjectDb.Serialization.Json;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Queries
{
    internal class QueryNodes : IQuery<Path, string, Node>, IQuery<Node, string, IEnumerable<Node>>, IQuery<Tree, Stack<string>, IEnumerable<Node>>
    {
        public Node Execute(Repository repository, Path path, string committish = null)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var commit = committish != null ?
                (Commit)repository.Lookup(committish) :
                repository.Head.Tip;
            var entry = commit.Tree[path.DataPath];
            return DefaultSerializer.Deserialize(
                entry.Target.Peel<Blob>().GetContentStream(),
                path).Node;
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
            return Execute(repository, tree, Path.ToStack(node?.Path));
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
                        var blob = nestedTree[FileSystemStorage.DataFile];
                        if (blob != null)
                        {
                            yield return DefaultSerializer.Deserialize(
                                blob.Target.Peel<Blob>().GetContentStream(),
                                Path.FromStack(stack)).Node;
                        }
                    }
                    stack.Pop();
                }
                stack.Pop();
            }
        }
    }
}
