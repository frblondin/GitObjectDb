using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Internal.Queries
{
    internal class QueryPaths : IQuery<QueryPaths.Parameters, IEnumerable<DataPath>>
    {
        public QueryPaths()
        {
        }

        public IEnumerable<DataPath> Execute(IConnectionInternal connection, Parameters parms)
        {
            var entries = new Stack<(Tree Tree, DataPath Path)>();
            var resources = new Stack<DataPath>();

            // Fetch direct resources
            if (parms.ParentPath is not null && IncludeResources(parms))
            {
                FetchNodeResources(parms.RelativeTree, parms.ParentPath, resources, parms.IsRecursive);

                foreach (var resource in resources)
                {
                    yield return resource;
                }
                resources.Clear();
            }

            FetchDirectChildren(parms.RelativeTree, parms.ParentPath, entries);

            while (entries.Count > 0)
            {
                var current = entries.Pop();
                if (IsOfType(current.Path, parms.Type))
                {
                    yield return current.Path;
                }
                if (current.Path.IsNode && parms.IsRecursive && IncludeResources(parms))
                {
                    FetchNodeResources(current.Tree, current.Path, resources, parms.IsRecursive);

                    foreach (var resource in resources)
                    {
                        yield return resource;
                    }
                    resources.Clear();
                }
                if (parms.IsRecursive)
                {
                    FetchDirectChildren(current.Tree, current.Path, entries);
                }
            }
        }

        private static bool IncludeResources(Parameters parms)
        {
            return parms.Type == null || parms.Type == typeof(Resource) || parms.Type == typeof(ITreeItem);
        }

        private static bool IsOfType(DataPath path, Type? type)
        {
            if (type == null || type == typeof(ITreeItem))
            {
                return true;
            }
            else
            {
                var folderName = DataPath.GetFolderName(type);
                return folderName.Equals(path.FolderParts[path.FolderParts.Length - 2], StringComparison.OrdinalIgnoreCase);
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

        private static void FetchNodeResources(Tree tree, DataPath nodePath, Stack<DataPath> entries, bool isRecursive)
        {
            var resourceChildTree = tree[FileSystemStorage.ResourceFolder]?.Target.Peel<Tree>();
            if (resourceChildTree != null)
            {
                var folderParts = new Stack<string>();
                FetchResources(resourceChildTree, nodePath, entries, folderParts, isRecursive);
            }
        }

        private static void FetchResources(Tree tree, DataPath nodePath, Stack<DataPath> entries, Stack<string> folderParts, bool isRecursive)
        {
            foreach (var item in tree)
            {
                switch (item.TargetType)
                {
                    case TreeEntryTargetType.Blob:
                        var resourcePath = nodePath.CreateResourcePath(
                            new DataPath(string.Join("/", folderParts), item.Name));
                        entries.Push(resourcePath);
                        break;
                    case TreeEntryTargetType.Tree when isRecursive:
                        folderParts.Push(item.Name);
                        FetchResources(item.Target.Peel<Tree>(), nodePath, entries, folderParts, isRecursive);
                        folderParts.Pop();
                        break;
                }
            }
        }

        internal class Parameters
        {
            public Parameters(Type? type, DataPath? parentPath, Tree tree, Tree relativeTree, bool isRecursive)
            {
                Type = type;
                ParentPath = parentPath;
                Tree = tree;
                RelativeTree = relativeTree;
                IsRecursive = isRecursive;
            }

            public Type? Type { get; }

            public DataPath? ParentPath { get; }

            public Tree Tree { get; }

            public Tree RelativeTree { get; }

            public bool IsRecursive { get; }
        }
    }
}