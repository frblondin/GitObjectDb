using GitDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.Internal.Queries;

internal class QueryItems(IAsyncQuery<LoadItem.Parameters, TreeItem?> loader,
                          IAsyncEnumerableQuery<QueryResources.Parameters, (DataPath Path, Resource Resource)> queryResources)
    : IAsyncEnumerableQuery<QueryItems.Parameters, (DataPath Path, TreeItem Item)>
{
    public async IAsyncEnumerable<(DataPath Path, TreeItem Item)> ExecuteAsync(IConnection queryAccessor, Parameters parms,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var entries = new Stack<Parameters>();

        // Fetch direct resources
        if (IncludeResources(parms, queryAccessor.Serializer))
        {
            var node = (Node)await LoadItemAsync(queryAccessor, parms).ConfigureAwait(false);
            var resources = GetResources(queryAccessor, node, parms);
            await foreach (var resource in resources.ConfigureAwait(false))
            {
                yield return (resource.Path, resource.Resource);
            }
        }

        await FetchDirectChildrenAsync(queryAccessor, parms, entries).ConfigureAwait(false);

        while (entries.Count > 0)
        {
            token.ThrowIfCancellationRequested();

            var entryParams = entries.Pop();
            var item = await LoadItemAsync(queryAccessor, entryParams).ConfigureAwait(false);
            if (IsOfType(queryAccessor, entryParams.ParentPath!, parms.Type))
            {
                yield return (entryParams.ParentPath!, item);
            }

            if (IncludeResources(entryParams, queryAccessor.Serializer) && entryParams.IsRecursive)
            {
                var resources = GetResources(queryAccessor, (Node)item, entryParams);
                await foreach (var resource in resources.ConfigureAwait(false))
                {
                    yield return (resource.Path, resource.Resource);
                }
            }

            if (parms.IsRecursive)
            {
                await FetchDirectChildrenAsync(queryAccessor, entryParams, entries).ConfigureAwait(false);
            }
        }
    }

    private async Task<TreeItem> LoadItemAsync(IConnection queryAccessor, Parameters parms) =>
        (await loader.ExecuteAsync(queryAccessor,
                             new LoadItem.Parameters(parms.Tree, parms.Index, parms.ParentPath!)).ConfigureAwait(false))!;

    private IAsyncEnumerable<(DataPath Path, Resource Resource)> GetResources(IConnection queryAccessor,
                                                                               Node node,
                                                                               Parameters parms) =>
        queryResources.ExecuteAsync(queryAccessor,
                               new QueryResources.Parameters(parms.Tree, parms.RelativeTree, node));

    private static bool IncludeResources(Parameters parms, INodeSerializer serializer) =>
        (parms.Type == null || parms.Type == typeof(Resource) || parms.Type == typeof(TreeItem)) &&
        parms.ParentPath is not null && parms.ParentPath.IsNode(serializer);

    private static bool IsOfType(IConnection queryAccessor, DataPath path, Type? type)
    {
        if (type == null || type == typeof(TreeItem) || type == typeof(Node))
        {
            return true;
        }
        else
        {
            var nodeFolderName = path.UseNodeFolders ? path.FolderParts[^2] : path.FolderParts[^1];
            return queryAccessor.Model.GetTypesMatchingFolderName(nodeFolderName).Any(
                typeDescription => type.IsAssignableFrom(typeDescription.Type));
        }
    }

    private static async Task FetchDirectChildrenAsync(IConnection queryAccessor, Parameters parameters, Stack<Parameters> entries)
    {
        await FetchDirectChildrenStoredInNestedFolderAsync(queryAccessor, parameters, entries).ConfigureAwait(false);
        await FetchDirectChildrenStoredWithoutNestedFolderAsync(queryAccessor, parameters, entries).ConfigureAwait(false);
    }

    private static async Task FetchDirectChildrenStoredWithoutNestedFolderAsync(
        IConnection queryAccessor, Parameters parameters, Stack<Parameters> entries)
    {
        foreach (var childTree in from folderChildTree in parameters.RelativeTree.Children.Where(e => e.Mode.Type == ObjectType.Tree)
                                  where folderChildTree.Name != FileSystemStorage.ResourceFolder
                                  select folderChildTree)
        {
            var nestedTree = await childTree.GetEntryAsync<TreeEntry>().ConfigureAwait(false);
            foreach (var child in nestedTree.Children.Where(e => e.Mode.Type == ObjectType.RegularFile))
            {
                if (UniqueId.TryParse(Path.GetFileNameWithoutExtension(child.Name), out var id))
                {
                    var childPath = parameters.ParentPath?.AddChild(childTree.Name, id, false, queryAccessor.Serializer.FileExtension) ??
                        DataPath.Root(childTree.Name, id, false, queryAccessor.Serializer.FileExtension);
                    entries.Push(parameters with { RelativeTree = nestedTree, ParentPath = childPath });
                }
            }
        }
    }

    private static async Task FetchDirectChildrenStoredInNestedFolderAsync(IConnection queryAccessor, Parameters parameters, Stack<Parameters> entries)
    {
        foreach (var childTree in from childTree in parameters.RelativeTree.Children.Where(e => e.Mode.Type == ObjectType.Tree)
                                  where childTree.Name != FileSystemStorage.ResourceFolder
                                  select childTree)
        {
            var nestedTree = await childTree.GetEntryAsync<TreeEntry>().ConfigureAwait(false);
            foreach (var nestedChildTree in from nestedChildTree in nestedTree.Children.Where(e => e.Mode.Type == ObjectType.Tree)
                                            where nestedChildTree.Name != FileSystemStorage.ResourceFolder
                                            select nestedChildTree)
            {
                if (UniqueId.TryParse(Path.GetFileNameWithoutExtension(nestedChildTree.Name), out var id))
                {
                    var nestedTree2 = await nestedChildTree.GetEntryAsync<TreeEntry>().ConfigureAwait(false);
                    if (nestedTree2.Children.Any(e => e.Name == $"{id}.{queryAccessor.Serializer.FileExtension}"))
                    {
                        var childPath = parameters.ParentPath?.AddChild(childTree.Name, id, true, queryAccessor.Serializer.FileExtension) ??
                            DataPath.Root(childTree.Name, id, true, queryAccessor.Serializer.FileExtension);
                        entries.Push(parameters with { RelativeTree = nestedTree2, ParentPath = childPath });
                    }
                }
            }
        }
    }

    internal record struct Parameters(TreeEntry Tree,
                                      TreeEntry RelativeTree,
                                      IIndex? Index,
                                      Type? Type,
                                      DataPath? ParentPath,
                                      bool IsRecursive);
}
