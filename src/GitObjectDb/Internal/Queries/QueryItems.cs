using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitObjectDb.Internal.Queries;

internal class QueryItems : IQuery<QueryItems.Parameters, IEnumerable<(DataPath Path, Lazy<ITreeItem> Item)>>
{
    private readonly IQuery<LoadItem.Parameters, ITreeItem> _loader;
    private readonly IQuery<QueryResources.Parameters, IEnumerable<(DataPath Path, Lazy<Resource> Resource)>> _queryResources;

    public QueryItems(IQuery<LoadItem.Parameters, ITreeItem> loaderFromLibGit2,
                      IQuery<QueryResources.Parameters, IEnumerable<(DataPath Path, Lazy<Resource> Resource)>> queryResources)
    {
        _loader = loaderFromLibGit2;
        _queryResources = queryResources;
    }

    public IEnumerable<(DataPath Path, Lazy<ITreeItem> Item)> Execute(IQueryAccessor queryAccessor, Parameters parms)
    {
        var entries = new Stack<Parameters>();
        var includeResources = IncludeResources(parms);

        // Fetch direct resources
        if (parms.ParentPath is not null && includeResources)
        {
            var resources = GetResources(queryAccessor, parms);
            foreach (var resource in resources)
            {
                yield return (resource.Path, new Lazy<ITreeItem>(() => resource.Resource.Value));
            }
        }

        FetchDirectChildren(parms, entries);

        while (entries.Count > 0)
        {
            var entryParams = entries.Pop();
            if (IsOfType(queryAccessor, entryParams.ParentPath!, parms.Type))
            {
                yield return (entryParams.ParentPath!, LoadItem(queryAccessor, entryParams));
            }

            if (entryParams.ParentPath!.IsNode && parms.IsRecursive && includeResources)
            {
                var resources = GetResources(queryAccessor, entryParams);
                foreach (var resource in resources)
                {
                    yield return (resource.Path, new Lazy<ITreeItem>(() => resource.Resource.Value));
                }
            }

            if (parms.IsRecursive)
            {
                FetchDirectChildren(entryParams, entries);
            }
        }
    }

    private Lazy<ITreeItem> LoadItem(IQueryAccessor queryAccessor, Parameters parms) =>
        new(() => _loader.Execute(queryAccessor,
                                  new LoadItem.Parameters(parms.Tree,
                                                          parms.ParentPath!,
                                                          parms.ReferenceCache)));

    private IEnumerable<(DataPath Path, Lazy<Resource> Resource)> GetResources(IQueryAccessor queryAccessor, Parameters parms) =>
        _queryResources.Execute(queryAccessor,
                                new QueryResources.Parameters(parms.Tree,
                                                              parms.RelativeTree,
                                                              parms.ParentPath!,
                                                              parms.ReferenceCache));

    private static bool IncludeResources(Parameters parms) =>
        parms.Type == null || parms.Type == typeof(Resource) || parms.Type == typeof(ITreeItem);

    private static bool IsOfType(IQueryAccessor queryAccessor, DataPath path, Type? type)
    {
        if (type == null || type == typeof(ITreeItem) || type == typeof(Node))
        {
            return true;
        }
        else
        {
            var nodeFolderName = path.UseNodeFolders ?
                                 path.FolderParts[path.FolderParts.Length - 2] :
                                 path.FolderParts[path.FolderParts.Length - 1];
            return queryAccessor.Model.GetTypesMatchingFolderName(nodeFolderName).Any(
                typeDescription => type.IsAssignableFrom(typeDescription.Type));
        }
    }

    private static void FetchDirectChildren(Parameters parameters, Stack<Parameters> entries)
    {
        FetchDirectChildrenStoredInNestedFolder(parameters, entries);
        FetchDirectChildrenStoredWithoutNestedFolder(parameters, entries);
    }

    private static void FetchDirectChildrenStoredWithoutNestedFolder(Parameters parameters, Stack<Parameters> entries)
    {
        UniqueId id = default;
        foreach (var info in from folderChildTree in parameters.RelativeTree.Where(e => e.TargetType == TreeEntryTargetType.Tree)
                             where folderChildTree.Name != FileSystemStorage.ResourceFolder
                             let nestedTree = folderChildTree.Target.Peel<Tree>()
                             from childFile in nestedTree.Where(e => e.TargetType == TreeEntryTargetType.Blob)
                             where UniqueId.TryParse(Path.GetFileNameWithoutExtension(childFile.Name), out id)
                             let childPath =
                                 parameters.ParentPath?.AddChild(folderChildTree.Name, id, false) ??
                                 DataPath.Root(folderChildTree.Name, id, false)
                             select parameters with { RelativeTree = nestedTree, ParentPath = childPath })
        {
            entries.Push(info);
        }
    }

    private static void FetchDirectChildrenStoredInNestedFolder(Parameters parameters, Stack<Parameters> entries)
    {
        UniqueId id = default;
        foreach (var info in from folderChildTree in parameters.RelativeTree.Where(e => e.TargetType == TreeEntryTargetType.Tree)
                             where folderChildTree.Name != FileSystemStorage.ResourceFolder
                             from childFolder in folderChildTree.Target.Peel<Tree>().Where(e => e.TargetType == TreeEntryTargetType.Tree)
                             where UniqueId.TryParse(childFolder.Name, out id)
                             let nestedTree = childFolder.Target.Peel<Tree>()
                             where nestedTree.Any(e => e.Name == $"{id}.json")
                             let childPath =
                                 parameters.ParentPath?.AddChild(folderChildTree.Name, id, true) ??
                                 DataPath.Root(folderChildTree.Name, id, true)
                             select parameters with { RelativeTree = nestedTree, ParentPath = childPath })
        {
            entries.Push(info);
        }
    }

    internal record Parameters
    {
        public Parameters(Tree tree,
                          Tree relativeTree,
                          Type? type,
                          DataPath? parentPath,
                          bool isRecursive,
                          IMemoryCache referenceCache)
        {
            Tree = tree;
            RelativeTree = relativeTree;
            Type = type;
            ParentPath = parentPath;
            IsRecursive = isRecursive;
            ReferenceCache = referenceCache;
        }

        public Tree Tree { get; }

        public Tree RelativeTree { get; init; }

        public Type? Type { get; }

        public DataPath? ParentPath { get; init; }

        public bool IsRecursive { get; }

        public IMemoryCache ReferenceCache { get; }
    }
}
