using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitObjectDb.Internal.Queries;

internal class QueryItems : IQuery<QueryItems.Parameters, IEnumerable<(DataPath Path, Lazy<TreeItem> Item)>>
{
    private readonly IQuery<LoadItem.Parameters, TreeItem?> _loader;
    private readonly IQuery<QueryResources.Parameters, IEnumerable<(DataPath Path, Lazy<Resource> Resource)>> _queryResources;

    public QueryItems(IQuery<LoadItem.Parameters, TreeItem?> loader,
                      IQuery<QueryResources.Parameters, IEnumerable<(DataPath Path, Lazy<Resource> Resource)>> queryResources)
    {
        _loader = loader;
        _queryResources = queryResources;
    }

    public IEnumerable<(DataPath Path, Lazy<TreeItem> Item)> Execute(IQueryAccessor queryAccessor, Parameters parms)
    {
        var entries = new Stack<Parameters>();

        // Fetch direct resources
        if (IncludeResources(parms))
        {
            var node = (Node)LoadItem(queryAccessor, parms).Value;
            var resources = GetResources(queryAccessor, node, parms);
            foreach (var resource in resources)
            {
                yield return (resource.Path, new Lazy<TreeItem>(() => resource.Resource.Value));
            }
        }

        FetchDirectChildren(queryAccessor, parms, entries);

        while (entries.Count > 0)
        {
            var entryParams = entries.Pop();
            var lazyItem = LoadItem(queryAccessor, entryParams);
            if (IsOfType(queryAccessor, entryParams.ParentPath!, parms.Type))
            {
                yield return (entryParams.ParentPath!, lazyItem);
            }

            if (IncludeResources(entryParams) && entryParams.IsRecursive)
            {
                var resources = GetResources(queryAccessor, (Node)lazyItem.Value, entryParams);
                foreach (var resource in resources)
                {
                    yield return (resource.Path, new Lazy<TreeItem>(() => resource.Resource.Value));
                }
            }

            if (parms.IsRecursive)
            {
                FetchDirectChildren(queryAccessor, entryParams, entries);
            }
        }
    }

    private Lazy<TreeItem> LoadItem(IQueryAccessor queryAccessor, Parameters parms) =>
        new(() => _loader.Execute(queryAccessor,
                                  new LoadItem.Parameters(parms.Tree, parms.Index, parms.ParentPath!))!);

    private IEnumerable<(DataPath Path, Lazy<Resource> Resource)> GetResources(IQueryAccessor queryAccessor,
                                                                               Node node,
                                                                               Parameters parms) =>
        _queryResources.Execute(queryAccessor,
                                new QueryResources.Parameters(parms.Tree, parms.RelativeTree, node));

    private static bool IncludeResources(Parameters parms) =>
        (parms.Type == null || parms.Type == typeof(Resource) || parms.Type == typeof(TreeItem)) &&
        parms.ParentPath is not null && parms.ParentPath.IsNode;

    private static bool IsOfType(IQueryAccessor queryAccessor, DataPath path, Type? type)
    {
        if (type == null || type == typeof(TreeItem) || type == typeof(Node))
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

    private static void FetchDirectChildren(IQueryAccessor queryAccessor, Parameters parameters, Stack<Parameters> entries)
    {
        FetchDirectChildrenStoredInNestedFolder(queryAccessor, parameters, entries);
        FetchDirectChildrenStoredWithoutNestedFolder(queryAccessor, parameters, entries);
    }

    private static void FetchDirectChildrenStoredWithoutNestedFolder(IQueryAccessor queryAccessor, Parameters parameters, Stack<Parameters> entries)
    {
        UniqueId id = default;
        foreach (var info in from folderChildTree in parameters.RelativeTree.Where(e => e.TargetType == TreeEntryTargetType.Tree)
                             where folderChildTree.Name != FileSystemStorage.ResourceFolder
                             let nestedTree = folderChildTree.Target.Peel<Tree>()
                             from childFile in nestedTree.Where(e => e.TargetType == TreeEntryTargetType.Blob)
                             where UniqueId.TryParse(Path.GetFileNameWithoutExtension(childFile.Name), out id)
                             let childPath =
                                 parameters.ParentPath?.AddChild(folderChildTree.Name, id, false, queryAccessor.Serializer.FileExtension) ??
                                 DataPath.Root(folderChildTree.Name, id, false, queryAccessor.Serializer.FileExtension)
                             select parameters with { RelativeTree = nestedTree, ParentPath = childPath })
        {
            entries.Push(info);
        }
    }

    private static void FetchDirectChildrenStoredInNestedFolder(IQueryAccessor queryAccessor, Parameters parameters, Stack<Parameters> entries)
    {
        UniqueId id = default;
        foreach (var info in from folderChildTree in parameters.RelativeTree.Where(e => e.TargetType == TreeEntryTargetType.Tree)
                             where folderChildTree.Name != FileSystemStorage.ResourceFolder
                             from childFolder in folderChildTree.Target.Peel<Tree>().Where(e => e.TargetType == TreeEntryTargetType.Tree)
                             where UniqueId.TryParse(childFolder.Name, out id)
                             let nestedTree = childFolder.Target.Peel<Tree>()
                             where nestedTree.Any(e => e.Name == $"{id}.{queryAccessor.Serializer.FileExtension}")
                             let childPath =
                                 parameters.ParentPath?.AddChild(folderChildTree.Name, id, true, queryAccessor.Serializer.FileExtension) ??
                                 DataPath.Root(folderChildTree.Name, id, true, queryAccessor.Serializer.FileExtension)
                             select parameters with { RelativeTree = nestedTree, ParentPath = childPath })
        {
            entries.Push(info);
        }
    }

    internal record struct Parameters(Tree Tree,
                                      Tree RelativeTree,
                                      IIndex? Index,
                                      Type? Type,
                                      DataPath? ParentPath,
                                      bool IsRecursive);
}
