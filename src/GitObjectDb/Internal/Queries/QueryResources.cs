using GitDotNet;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.Internal.Queries;

internal class QueryResources(IAsyncQuery<QuerySubModules.Parameters, CommitEntry> querySubModules)
    : IAsyncEnumerableQuery<QueryResources.Parameters, (DataPath Path, Resource Resource)>
{
    public async IAsyncEnumerable<(DataPath Path, Resource Resource)> ExecuteAsync(IConnection queryAccessor,
        Parameters parms,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var nodePath = parms.Node.Path ??
            throw new GitObjectDbException("Node has no path defined.");
        var refResourceTreeItem = await parms.RelativeTree.GetFromPathAsync(FileSystemStorage.ResourceFolder).ConfigureAwait(false);

        if (refResourceTreeItem?.Mode.Type == ObjectType.Tree)
        {
            var refResourceTree = await refResourceTreeItem.GetEntryAsync<TreeEntry>().ConfigureAwait(false);
            var traversed = refResourceTree.GetAllBlobEntriesAsync(new($"{nodePath.FolderPath}/{FileSystemStorage.ResourceFolder}"), token);
            await foreach (var resource in ResolveResourcesAsync(traversed, token).ConfigureAwait(false))
            {
                yield return resource;
            }
        }

        if (parms.Node.RemoteResource is not null &&
            refResourceTreeItem?.Mode.Type == ObjectType.GitLink)
        {
            var commit = await querySubModules.ExecuteAsync(queryAccessor, new(parms.Node)).ConfigureAwait(false);
            var tree = await commit.GetRootTreeAsync().ConfigureAwait(false);
            var traversed = tree.GetAllBlobEntriesAsync($"{nodePath.FolderPath}/{FileSystemStorage.ResourceFolder}", token);
            await foreach (var resource in ResolveResourcesAsync(traversed, token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                yield return resource;
            }
        }
    }

    private static async IAsyncEnumerable<(DataPath Path, Resource Resource)> ResolveResourcesAsync(
        IAsyncEnumerable<(GitPath Path, TreeEntryItem Item)> traversed, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var (path, item) in traversed.ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();

            if (item.Mode.Type == ObjectType.RegularFile)
            {
                var dataPath = DataPath.Parse(path);
                var blob = await item.GetEntryAsync<BlobEntry>().ConfigureAwait(false);
                yield return
                (
                    dataPath,
                    new Resource(dataPath, new Resource.Data(() => Task.FromResult(blob.OpenRead())))
                );
            }
        }
    }

    internal record struct Parameters(TreeEntry Tree, TreeEntry RelativeTree, Node Node);
}
