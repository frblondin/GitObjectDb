using GitObjectDb.Serialization;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Internal.Queries
{
    internal class QueryItems : IQuery<QueryItems.Parameters, IEnumerable<(DataPath Path, Lazy<ITreeItem> Item)>>
    {
        private readonly INodeSerializer _serializer;
        private readonly IQuery<LoadItem.Parameters, ITreeItem> _loader;
        private readonly IQuery<QueryResources.Parameters, IEnumerable<(DataPath Path, Lazy<Resource> Resource)>> _queryResources;

        public QueryItems(INodeSerializer serializer, IQuery<LoadItem.Parameters, ITreeItem> loader, IQuery<QueryResources.Parameters, IEnumerable<(DataPath Path, Lazy<Resource> Resource)>> queryResources)
        {
            _serializer = serializer;
            _loader = loader;
            _queryResources = queryResources;
        }

        public IEnumerable<(DataPath Path, Lazy<ITreeItem> Item)> Execute(IConnectionInternal connection, Parameters parms)
        {
            var entries = new Stack<(Tree RelativeTree, DataPath Path)>();
            var includeResources = IncludeResources(parms);

            // Fetch direct resources
            if (parms.ParentPath is not null && includeResources)
            {
                var resources = _queryResources.Execute(connection, new QueryResources.Parameters(parms.RelativeTree, parms.ParentPath!));
                foreach (var resource in resources)
                {
                    yield return (resource.Path, new Lazy<ITreeItem>(() => resource.Resource.Value));
                }
            }

            FetchDirectChildren(parms.RelativeTree, parms.ParentPath, entries);

            while (entries.Count > 0)
            {
                var current = entries.Pop();
                if (IsOfType(connection, current.Path, parms.Type))
                {
                    var blob = current.RelativeTree[current.Path.FileName].Target.Peel<Blob>();
                    using var stream = blob.GetContentStream();
                    yield return
                    (
                        current.Path,
                        new Lazy<ITreeItem>(
                            () => _serializer.Deserialize(stream,
                                                          current.Path,
                                                          blob.Id.Sha,
                                                          ResolveReference).Node)
                    );
                }

                if (current.Path.IsNode && parms.IsRecursive && includeResources)
                {
                    var resources = _queryResources.Execute(connection,
                                                            new QueryResources.Parameters(current.RelativeTree, current.Path));
                    foreach (var resource in resources)
                    {
                        yield return (resource.Path, new Lazy<ITreeItem>(() => resource.Resource.Value));
                    }
                }

                if (parms.IsRecursive)
                {
                    FetchDirectChildren(current.RelativeTree, current.Path, entries);
                }
            }

            ITreeItem ResolveReference(DataPath path) =>
                _loader.Execute(connection, parms.ToLoadItemParameter(path));
        }

        private static bool IncludeResources(Parameters parms) =>
            parms.Type == null || parms.Type == typeof(Resource) || parms.Type == typeof(ITreeItem);

        private static bool IsOfType(IConnection connection, DataPath path, Type? type)
        {
            if (type == null || type == typeof(ITreeItem) || type == typeof(Node))
            {
                return true;
            }
            else
            {
                var nodeFolderName = path.UseNodeFolders ? path.FolderParts[path.FolderParts.Length - 2] : path.FolderParts[path.FolderParts.Length - 1];
                return connection.Model.GetTypesMatchingFolderName(nodeFolderName).Any(
                    typeDescription => type.IsAssignableFrom(typeDescription.Type));
            }
        }

        private static void FetchDirectChildren(Tree tree, DataPath? parentPath, Stack<(Tree NestedTree, DataPath ChildPath)> entries)
        {
            FetchDirectChildrenStoredInNestedFolder(tree, parentPath, entries);
            FetchDirectChildrenStoredWithoutNestedFolder(tree, parentPath, entries);
        }

        private static void FetchDirectChildrenStoredWithoutNestedFolder(Tree tree, DataPath? parentPath, Stack<(Tree NestedTree, DataPath ChildPath)> entries)
        {
            UniqueId id = default;
            foreach (var info in from folderChildTree in tree.Where(e => e.TargetType == TreeEntryTargetType.Tree)
                                 let nestedTree = folderChildTree.Target.Peel<Tree>()
                                 from childFile in nestedTree.Where(e => e.TargetType == TreeEntryTargetType.Blob)
                                 where UniqueId.TryParse(System.IO.Path.GetFileNameWithoutExtension(childFile.Name), out id)
                                 let childPath =
                                     parentPath?.AddChild(folderChildTree.Name, id, false) ??
                                     DataPath.Root(folderChildTree.Name, id, false)
                                 select (nestedTree, childPath))
            {
                entries.Push(info);
            }
        }

        private static void FetchDirectChildrenStoredInNestedFolder(Tree tree, DataPath? parentPath, Stack<(Tree NestedTree, DataPath ChildPath)> entries)
        {
            UniqueId id = default;
            foreach (var info in from folderChildTree in tree.Where(e => e.TargetType == TreeEntryTargetType.Tree)
                                 from childFolder in folderChildTree.Target.Peel<Tree>().Where(e => e.TargetType == TreeEntryTargetType.Tree)
                                 where UniqueId.TryParse(childFolder.Name, out id)
                                 let nestedTree = childFolder.Target.Peel<Tree>()
                                 where nestedTree.Any(e => e.Name == $"{id}.json")
                                 let childPath =
                                     parentPath?.AddChild(folderChildTree.Name, id, true) ??
                                     DataPath.Root(folderChildTree.Name, id, true)
                                 select (nestedTree, childPath))
            {
                entries.Push(info);
            }
        }

        internal class Parameters
        {
            public Parameters(Type? type, DataPath? parentPath, Tree tree, Tree relativeTree, bool isRecursive, IDictionary<DataPath, ITreeItem>? referenceCache)
            {
                Type = type;
                ParentPath = parentPath;
                Tree = tree;
                RelativeTree = relativeTree;
                IsRecursive = isRecursive;
                ReferenceCache = referenceCache;
            }

            public Type? Type { get; }

            public DataPath? ParentPath { get; }

            public Tree Tree { get; }

            public Tree RelativeTree { get; }

            public bool IsRecursive { get; }

            public IDictionary<DataPath, ITreeItem>? ReferenceCache { get; }

            public LoadItem.Parameters ToLoadItemParameter(DataPath path) =>
                new LoadItem.Parameters(Tree, path, ReferenceCache);
        }
    }
}