using GitObjectDb.Serialization;
using GitObjectDb.Tools;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Internal.Queries
{
    internal class QueryNodes : IQuery<QueryNodes.Parameters, IEnumerable<Node>>
    {
        private readonly INodeSerializer _serializer;
        private readonly IQuery<LoadItem.Parameters, ITreeItem> _loader;

        public QueryNodes(INodeSerializer serializer, IQuery<LoadItem.Parameters, ITreeItem> loader)
        {
            _serializer = serializer;
            _loader = loader;
        }

        public IEnumerable<Node> Execute(IConnectionInternal connection, Parameters parms)
        {
            var entries = new Stack<(Tree Tree, DataPath Path)>();
            var folderNames = parms.Type == typeof(Node) ?
                null :
                new HashSet<string>(
                    TypeHelper.GetDerivedTypesIncludingSelf(parms.Type)
                    .Select(DataPath.GetFolderName));
            FetchDirectChildren(parms.RelativeTree, parms.Parent?.Path, entries);

            while (entries.Count > 0)
            {
                var current = entries.Pop();
                if (IsOfType(current.Path, folderNames))
                {
                    var blob = current.Tree[current.Path.FileName].Target.Peel<Blob>();
                    using var stream = blob.GetContentStream();
                    yield return _serializer.Deserialize(stream,
                                                         current.Path,
                                                         blob.Id.Sha,
                                                         ResolveReference).Node;
                }

                if (parms.IsRecursive)
                {
                    FetchDirectChildren(current.Tree, current.Path, entries);
                }
            }

            ITreeItem ResolveReference(DataPath path) =>
                _loader.Execute(connection, parms.ToLoadItemParameter(path));
        }

        private static bool IsOfType(DataPath path, ISet<string>? folderNames)
        {
            if (folderNames == null)
            {
                return true;
            }
            else
            {
                return folderNames.Contains(path.FolderParts[path.FolderParts.Length - 2]);
            }
        }

        private static void FetchDirectChildren(Tree tree, DataPath? parentPath, Stack<(Tree NestedTree, DataPath ChildPath)> entries)
        {
            UniqueId id = default;
            foreach (var info in from folderChildTree in tree.Where(e => e.TargetType == TreeEntryTargetType.Tree)
                                 from childFolder in folderChildTree.Target.Peel<Tree>().Where(e => e.TargetType == TreeEntryTargetType.Tree)
                                 where UniqueId.TryParse(childFolder.Name, out id)
                                 let nestedTree = childFolder.Target.Peel<Tree>()
                                 where nestedTree.Any(e => e.Name == FileSystemStorage.DataFile)
                                 let childPath =
                                     parentPath?.AddChild(folderChildTree.Name, id) ??
                                     DataPath.Root(folderChildTree.Name, id)
                                 select (nestedTree, childPath))
            {
                entries.Push(info);
            }
        }

        internal class Parameters
        {
            public Parameters(Type type, Node? parent, Tree tree, Tree relativeTree, bool isRecursive, IDictionary<DataPath, ITreeItem>? referenceCache)
            {
                Type = type;
                Parent = parent;
                Tree = tree;
                RelativeTree = relativeTree;
                IsRecursive = isRecursive;
                ReferenceCache = referenceCache;
            }

            public Type Type { get; }

            public Node? Parent { get; }

            public Tree Tree { get; }

            public Tree RelativeTree { get; }

            public bool IsRecursive { get; }

            public IDictionary<DataPath, ITreeItem>? ReferenceCache { get; }

            public LoadItem.Parameters ToLoadItemParameter(DataPath path) =>
                new LoadItem.Parameters(Tree, path, ReferenceCache);
        }
    }
}
