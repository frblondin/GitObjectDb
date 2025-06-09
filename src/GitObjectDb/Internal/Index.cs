using Fasterflect;
using GitDotNet;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Internal.Queries;
using GitObjectDb.Tools;
using Realms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static GitObjectDb.Internal.Commands.GitUpdateCommand;

namespace GitObjectDb.Internal;
internal partial class Index : IIndex
{
    private readonly IConnection _connection;
    private readonly IAsyncQuery<LoadItem.Parameters, TreeItem?> _loader;

    private Index(IConnection connection,
        string branchName,
        ICommitCommand commitCommand,
        IAsyncQuery<LoadItem.Parameters, TreeItem?> loader)
    {
        Configuration = new(Path.Combine(connection.Repository.Info.Path, $"{branchName.Replace("/", "__")}.index"));
        _connection = connection;
        BranchName = branchName;
        _commitCommand = commitCommand;
        _loader = loader;
    }

    internal RealmConfiguration Configuration { get; }

    public HashId? CommitId
    {
        get
        {
            using var realm = Realm.GetInstance(Configuration);
            var info = realm.All<IndexInfoRealm>().AsEnumerable().FirstOrDefault();
            return info?.CommitId != null && HashId.TryParse(info.CommitId, out var result) ? result : null;
        }
    }

    public Guid? Version
    {
        get
        {
            using var realm = Realm.GetInstance(Configuration);
            return realm.All<IndexInfoRealm>().FirstOrDefault()?.Version;
        }
    }

    public string BranchName { get; }

    public int Count
    {
        get
        {
            using var realm = Realm.GetInstance(Configuration);
            return realm.All<IndexEntry>().Count();
        }
    }

    public IndexEntry this[DataPath path] =>
        TryLoadEntry(path) ?? throw new KeyNotFoundException("Path could not be found.");

    [FactoryDelegate(typeof(Factories.IndexFactory))]
    public static async Task<IIndex> CreateAsync(IConnection connection,
        string branchName,
        ICommitCommand commitCommand,
        IAsyncQuery<LoadItem.Parameters, TreeItem?> loader)
    {
        var result = new Index(connection, branchName, commitCommand, loader);
        await result.GetAndVerifyBranchTipAsync().ConfigureAwait(false);
        return result;
    }

    public void Reset()
    {
        try
        {
            Realm.DeleteRealm(Configuration);
        }
        catch
        {
            using var realm = Realm.GetInstance(Configuration);
            realm.Write(realm.RemoveAll);
        }
    }

    public IEnumerator<IndexEntry> GetEnumerator()
    {
        using var realm = Realm.GetInstance(Configuration);
        return realm.All<IndexEntry>()
            .AsEnumerable() // Select is not supported by Realm queryable implementation
            .Select(e => e.Freeze()) // Make entries accessible offline
            .ToList() // Force projection of query
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private async Task<CommitEntry> GetAndVerifyBranchTipAsync()
    {
        var branch = _connection.Repository.Branches[BranchName] ??
            throw new GitObjectDbException($"Branch {BranchName} does not exist.");
        var result = await branch.GetTipAsync().ConfigureAwait(false);
        var existingTip = CommitId;
        if (existingTip is not null && existingTip != result.Id)
        {
            throw new GitObjectDbException($"Changes have been made to Index.");
        }
        return result;
    }

    public async Task UpdateToBranchTipAsync()
    {
        var branch = _connection.Repository.Branches[BranchName] ??
            throw new GitObjectDbException($"Branch {BranchName} does not exist.");
        var branchTip = await branch.GetTipAsync().ConfigureAwait(false);
        using var realm = Realm.GetInstance(Configuration);
        realm.Write(() => IncrementVersion(realm, branchTip.Id));
    }

    public async Task<TNode> CreateOrUpdateAsync<TNode>(TNode node, Node? parent)
        where TNode : Node =>
        await UpsertOrDeleteItemAsync(node, parent?.ThrowIfNoPath(), delete: false).ConfigureAwait(false);

    public async Task<TNode> CreateOrUpdateAsync<TNode>(TNode node, DataPath? parent)
        where TNode : Node =>
        await UpsertOrDeleteItemAsync(node, parent, delete: false).ConfigureAwait(false);

    public async Task<TNode> CreateOrUpdateAsync<TNode>(TNode node)
        where TNode : Node =>
        await UpsertOrDeleteItemAsync(node, null, delete: false).ConfigureAwait(false);

    public async Task<Resource> CreateOrUpdateAsync(Resource resource) =>
        await UpsertOrDeleteItemAsync(resource, default, false).ConfigureAwait(false);

    public async Task RenameAsync(TreeItem item, DataPath newPath)
    {
        var newItem = ValidateRename(item, newPath, _connection.Serializer);

        await DeleteAsync(item).ConfigureAwait(false);
        await UpsertOrDeleteItemAsync(newItem, default, false).ConfigureAwait(false);
    }

    public async Task DeleteAsync<TItem>(TItem item)
        where TItem : TreeItem =>
        await UpsertOrDeleteItemAsync(item, default, true).ConfigureAwait(false);

    public Task RevertAsync(DataPath path)
    {
        if (CommitId == null)
        {
            return Task.CompletedTask;
        }
        using var realm = Realm.GetInstance(Configuration);
        var entry = TryLoadEntry(path, realm);
        if (entry is not null)
        {
            realm.Write(() =>
            {
                realm.Remove(entry);
                IncrementVersion(realm, CommitId);
            });
        }
        return Task.CompletedTask;
    }

    private async Task<TItem> UpsertOrDeleteItemAsync<TItem>(TItem item, DataPath? parent, bool delete)
        where TItem : TreeItem
    {
        var commit = await GetAndVerifyBranchTipAsync().ConfigureAwait(false);
        var type = item.GetType();
        if (type.IsNode())
        {
            // Make sure that node type is defined in model
            _connection.Model.GetDescription(type);
        }

        var node = item as Node;
        var path = node is not null && !delete ?
            ChangeComposer.UpdateNodePathIfNeeded(node, parent, _connection) :
            item.ThrowIfNoPath();
        var remoteResource = node is not null && !delete ?
            node.RemoteResource :
            null;

        var data = await GetEntryDataAsync(item, delete).ConfigureAwait(false);
        var entry = new IndexEntry
        {
            PathAsString = path.FilePath,
            Type = item.GetType().AssemblyQualifiedName!,
            Delete = delete,
            RemoteResourceRepository = remoteResource?.Repository,
            RemoteResourceSha = remoteResource?.Sha,
            Data = data,
        };
        AddPropertyStoredAsSeparateFiles(node, entry);

        using var realm = Realm.GetInstance(Configuration);
        realm.Write(() =>
        {
            realm.Add(entry, update: true);
            IncrementVersion(realm, commit.Id);
        });

        return item;
    }

    private void AddPropertyStoredAsSeparateFiles(Node? node, IndexEntry entry)
    {
        if (node == null)
        {
            return;
        }

        foreach (var property in _connection.Model.GetDescription(node.GetType()).StoredAsSeparateFilesProperties
                     .Select(info => info.Property))
        {
            var value = Reflect.PropertyGetter(property).Invoke(node)?.ToString();
            if (value != null)
            {
                entry.ExternalPropertyValues[property.Name] = value;
            }
        }
    }

    private static void IncrementVersion(Realm realm, HashId commitId)
    {
        realm.Add(new IndexInfoRealm()
        {
            CommitId = commitId.ToString(),
            Version = Guid.NewGuid(),
        }, update: true);
    }

    private async Task<byte[]?> GetEntryDataAsync(TreeItem item, bool delete)
    {
        if (delete)
        {
            return null;
        }
        else if (item is Resource resource)
        {
            return await resource.Embedded.GetBytesAsync().ConfigureAwait(false);
        }
        else if (item is Node node)
        {
            using var stream = _connection.Serializer.Serialize(node);
            using var reader = new BinaryReader(stream);
            return reader.ReadBytes((int)stream.Length);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public IndexEntry? TryLoadEntry(DataPath path)
    {
        using var realm = Realm.GetInstance(Configuration);
        return TryLoadEntry(path, realm)?.Freeze();
    }

    private static IndexEntry? TryLoadEntry(DataPath path, Realm realm) =>
        realm.All<IndexEntry>().FirstOrDefault(e => e.PathAsString.Equals((string?)path.FilePath, StringComparison.Ordinal));

    public async Task<TItem?> TryLoadItemAsync<TItem>(DataPath path, bool onlyIndex = false)
        where TItem : TreeItem
    {
        var tip = await GetAndVerifyBranchTipAsync().ConfigureAwait(false);
        var tree = await tip.GetRootTreeAsync().ConfigureAwait(false);
        return !onlyIndex || TryLoadEntry(path) is not null ?
            await _loader.ExecuteAsync(_connection, new(tree, this, path)).ConfigureAwait(false) as TItem :
            null;
    }

    public async Task<TreeItem> LoadItemAsync(IndexEntry entry)
    {
        var tip = await GetAndVerifyBranchTipAsync().ConfigureAwait(false);
        var tree = await tip.GetRootTreeAsync().ConfigureAwait(false);
        return await _loader.ExecuteAsync(_connection, new(tree, this, entry.Path!)).ConfigureAwait(false) ??
           throw new GitObjectDbException($"The entry for path {entry.Path} does not exist.");
    }

    internal partial class IndexInfoRealm : IRealmObject
    {
        [PrimaryKey]
        public string Id { get; private set; } = "$info";

        public string? CommitId { get; set; }

        public Guid Version { get; set; }
    }
}
