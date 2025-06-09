using GitDotNet;
using GitObjectDb.Comparison;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.Internal;

internal sealed partial class Connection
{
    // Use lazy for concurrent dictionary thread safety
    private readonly ConcurrentDictionary<DataPath, Lazy<IGitConnection>> _repositories = new();

    public async Task<TItem?> LookupAsync<TItem>(CommitEntry commit, DataPath path)
        where TItem : TreeItem
    {
        var tree = await commit.GetRootTreeAsync().ConfigureAwait(false);
        return await tree.GetFromPathAsync(path.FilePath).ConfigureAwait(false) is null ?
            default :
            await _loader.ExecuteAsync(this, new(tree, Index: null, path)).ConfigureAwait(false) as TItem;
    }

    public async Task<TItem?> LookupAsync<TItem>(CommitEntry commit, UniqueId id)
        where TItem : TreeItem
    {
        var path = await TryGetTreeAsync(commit, id).ConfigureAwait(false);
        return path is null ?
            default :
            await _loader.ExecuteAsync(this,
            new(await commit.GetRootTreeAsync().ConfigureAwait(false), Index: null, path)).ConfigureAwait(false) as TItem;
    }

    public async IAsyncEnumerable<TItem> GetItemsAsync<TItem>(CommitEntry commit,
        Node? parent = null,
        bool isRecursive = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TItem : TreeItem
    {
        var relativeTree = await TryGetTreeAsync(commit, parent?.Path).ConfigureAwait(false);
        if (relativeTree is not null)
        {
            var result = _queryItems.ExecuteAsync(this,
                new(await commit.GetRootTreeAsync().ConfigureAwait(false),
                relativeTree,
                Index: null,
                typeof(TItem),
                parent?.Path,
                isRecursive), cancellationToken).ConfigureAwait(false);
            await foreach (var item in result)
            {
                if (item.Item is TItem typed)
                {
                    yield return typed;
                }
            }
        }
    }

    public IAsyncEnumerable<TNode> GetNodesAsync<TNode>(CommitEntry commit,
        Node? parent = null,
        bool isRecursive = false,
        CancellationToken cancellationToken = default)
        where TNode : Node =>
        GetItemsAsync<TNode>(commit, parent, isRecursive, cancellationToken);

    public IAsyncEnumerable<DataPath> GetPathsAsync(CommitEntry commit,
        DataPath? parentPath = null,
        bool isRecursive = false,
        CancellationToken cancellationToken = default) =>
        GetPathsAsync<TreeItem>(commit, parentPath, isRecursive, cancellationToken);

    public async IAsyncEnumerable<DataPath> GetPathsAsync<TItem>(CommitEntry commit,
        DataPath? parentPath = null,
        bool isRecursive = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TItem : TreeItem
    {
        var relativeTree = await TryGetTreeAsync(commit, parentPath).ConfigureAwait(false);
        if (relativeTree is not null)
        {
            var result = _queryItems.ExecuteAsync(this, new(await commit.GetRootTreeAsync().ConfigureAwait(false),
                relativeTree,
                Index: null,
                typeof(TItem),
                parentPath,
                isRecursive),
                cancellationToken).Select(i => i.Path);
            await foreach (var entry in result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return entry;
            }
        }
    }

    public async IAsyncEnumerable<TreeItem> SearchAsync(CommitEntry commit,
        string pattern,
        DataPath? parentPath = null,
        bool ignoreCase = false,
        bool recurseSubModules = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var result = _searchItems.ExecuteAsync(this, new(this,
            await commit.GetRootTreeAsync().ConfigureAwait(false),
            pattern,
            parentPath,
            commit,
            ignoreCase,
            recurseSubModules),
            cancellationToken).ConfigureAwait(false);
        await foreach (var entry in result)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return entry.Item;
        }
    }

    public async IAsyncEnumerable<Resource> GetResourcesAsync(CommitEntry commit, Node node, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var relativeTree = await TryGetTreeAsync(commit, node.ThrowIfNoPath()).ConfigureAwait(false);
        if (relativeTree is not null)
        {
            var result = _queryResources.ExecuteAsync(this,
                new(await commit.GetRootTreeAsync().ConfigureAwait(false), relativeTree, node), cancellationToken).ConfigureAwait(false);
            await foreach (var entry in result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return entry.Resource;
            }
        }
    }

    public async Task<ChangeCollection> CompareAsync(string startCommittish,
        string committish,
        ComparisonPolicy? policy = null)
    {
        var old = await Repository.GetCommittishAsync(startCommittish).ConfigureAwait(false);
        var @new = await Repository.GetCommittishAsync(committish).ConfigureAwait(false);
        return await _comparer.CompareAsync(this, old, @new, policy ?? Model.DefaultComparisonPolicy).ConfigureAwait(false);
    }

    public IAsyncEnumerable<LogEntry> GetLogsAsync(CommitEntry commit, TreeItem item, CancellationToken cancellationToken = default) =>
        Repository.GetLogAsync(
            commit.Id.ToString(),
            LogOptions.Default with { Path = item.ThrowIfNoPath().FilePath },
            cancellationToken);

    private static async Task<TreeEntry?> TryGetTreeAsync(CommitEntry commit, DataPath? path = null)
    {
        var root = await commit.GetRootTreeAsync().ConfigureAwait(false);
        if (path is null || string.IsNullOrEmpty(path.FolderPath))
        {
            return root;
        }
        else
        {
            var tree = await root.GetFromPathAsync(path.FolderPath).ConfigureAwait(false);
            return tree != null ? await tree.GetEntryAsync<TreeEntry>().ConfigureAwait(false) : null;
        }
    }

    private async Task<DataPath?> TryGetTreeAsync(CommitEntry commit, UniqueId id)
    {
        var stack = new Stack<string>();
        var root = await commit.GetRootTreeAsync().ConfigureAwait(false);
        return await SearchAsync(root, $"{id}.{Serializer.FileExtension}", stack).ConfigureAwait(false) ?
            DataPath.Parse(string.Join("/", stack.Reverse())) :
            null;

        static async Task<bool> SearchAsync(TreeEntry tree, string blobName, Stack<string> path)
        {
            foreach (var item in tree.Children)
            {
                path.Push(item.Name);
                if (item.Mode.Type == ObjectType.RegularFile && item.Name == blobName)
                {
                    return true;
                }
                if (item.Mode.Type == ObjectType.Tree &&
                    !FileSystemStorage.IsResourceName(item.Name) &&
                    await SearchAsync(await item.GetEntryAsync<TreeEntry>(), blobName, path).ConfigureAwait(false))
                {
                    return true;
                }
                path.Pop();
            }
            return false;
        }
    }

    IGitConnection ISubmoduleProvider.GetOrCreateSubmoduleRepository(DataPath path, string url)
    {
        var folderPath = Path.Combine(Repository.Info.Path,
                                      "modules",
                                      path.FolderPath,
                                      FileSystemStorage.ResourceFolder);
        return _repositories.GetOrAdd(path,
            new Lazy<IGitConnection>(CreateOrLoad)).Value;
        IGitConnection CreateOrLoad() =>
            GitConnection.IsValid(folderPath) ? Load() : Create();

        IGitConnection Load() =>
            _connectionFactory(folderPath);

        IGitConnection Create()
        {
            Directory.CreateDirectory(folderPath);
            GitConnection.Clone(folderPath, url, new()
            {
                IsBare = true,
            });
            return Load();
        }
    }
}
