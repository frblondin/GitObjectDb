using Fasterflect;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Internal.Queries;
using GitObjectDb.Tools;
using LibGit2Sharp;
using Realms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using static GitObjectDb.Internal.Commands.GitUpdateCommand;

namespace GitObjectDb.Internal;
internal partial class Index : IIndex
{
    private readonly IConnectionInternal _connection;
    private readonly IQuery<LoadItem.Parameters, TreeItem?> _loader;

    private int _nestedCount;
    private Realm? _realm;

    [FactoryDelegateConstructor(typeof(Factories.IndexFactory))]
    public Index(IConnectionInternal connection,
                 string branchName,
                 ICommitCommand commitCommand,
                 IQuery<LoadItem.Parameters, TreeItem?> loader)
    {
        IndexStoragePath = Path.Combine(connection.Repository.Info.Path, $"{branchName.Replace("/", "__")}.index");
        _connection = connection;
        BranchName = branchName;
        _commitCommand = commitCommand;
        _loader = loader;

        GetAndVerifyBranchTip();
    }

    internal string IndexStoragePath { get; }

    public ObjectId? CommitId => DoRealmAction(
        realm => ObjectId.TryParse(realm.All<IndexInfoRealm>().FirstOrDefault()?.CommitId, out var result) ?
            result! :
            null);

    public Guid? Version => DoRealmAction(
        realm => realm.All<IndexInfoRealm>().FirstOrDefault()?.Version);

    public string BranchName { get; }

    public int Count => DoRealmAction(realm => realm.All<IndexEntry>().Count());

    public IndexEntry this[DataPath path] =>
        TryLoadEntry(path) ?? throw new KeyNotFoundException("Path could not be found.");

    public void Reset()
    {
        try
        {
            if (_realm is null)
            {
                Realm.DeleteRealm(new RealmConfiguration(IndexStoragePath));
            }
        }
        catch
        {
            DoRealmAction(realm => realm.Write(() => realm.RemoveAll()));
        }
    }

    public IEnumerator<IndexEntry> GetEnumerator() => DoRealmAction(realm => realm
        .All<IndexEntry>()
        .AsEnumerable() // Select is not supported by Realm queryable implementation
        .Select(e => e.Freeze()) // Make entries accessible offline
        .ToList() // Force projection of query
        .GetEnumerator());

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private Commit GetAndVerifyBranchTip()
    {
        var branch = _connection.Repository.Branches[BranchName] ??
            throw new GitObjectDbException($"Branch {BranchName} does not exist.");
        var result = branch.Tip;
        var existingTip = CommitId;
        if (existingTip is not null && existingTip != result.Id)
        {
            throw new GitObjectDbException($"Changes have been made to Index.");
        }
        return result;
    }

    public void UpdateToBranchTip()
    {
        var branch = _connection.Repository.Branches[BranchName] ??
            throw new GitObjectDbException($"Branch {BranchName} does not exist.");
        DoRealmAction(realm => realm.Write(() => IncrementVersion(realm, branch.Tip.Id)));
    }

    public TNode CreateOrUpdate<TNode>(TNode node, Node? parent)
        where TNode : Node =>
        UpsertOrDeleteItem(node, parent?.ThrowIfNoPath(), delete: false);

    public TNode CreateOrUpdate<TNode>(TNode node, DataPath? parent)
        where TNode : Node =>
        UpsertOrDeleteItem(node, parent, delete: false);

    public TNode CreateOrUpdate<TNode>(TNode node)
        where TNode : Node =>
        UpsertOrDeleteItem(node, null, delete: false);

    public Resource CreateOrUpdate(Resource resource) =>
        UpsertOrDeleteItem(resource, default, false);

    public void Rename(TreeItem item, DataPath newPath)
    {
        var newItem = ValidateRename(item, newPath, _connection.Serializer);

        Delete(item);
        UpsertOrDeleteItem(newItem, default, false);
    }

    public void Delete<TItem>(TItem item)
        where TItem : TreeItem =>
        UpsertOrDeleteItem(item, default, true);

    public void Revert(DataPath path)
    {
        var commit = GetAndVerifyBranchTip();
        DoRealmAction(realm =>
        {
            var entry = TryLoadEntry(path, realm);
            if (entry is not null)
            {
                realm.Write(() =>
                {
                    realm.Remove(entry);
                    IncrementVersion(realm, commit.Id);
                });
            }
        });
    }

    private TItem UpsertOrDeleteItem<TItem>(TItem item, DataPath? parent, bool delete)
        where TItem : TreeItem
    {
        var commit = GetAndVerifyBranchTip();
        var type = item.GetType();
        if (type.IsNode())
        {
            // Make sure that node type is defined in model
            _connection.Model.GetDescription(type);
        }

        var node = item as Node;
        var path = node is not null && !delete ?
            TransformationComposer.UpdateNodePathIfNeeded(node, parent, _connection) :
            item.ThrowIfNoPath();
        var remoteResource = node is not null && !delete ?
            node.RemoteResource :
            null;

        var data = GetEntryData(item, delete);
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

        DoRealmAction(realm =>
        {
            realm.Write(() =>
            {
                realm.Add(entry, update: true);
                IncrementVersion(realm, commit.Id);
            });
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

    private static void IncrementVersion(Realm realm, ObjectId commitId)
    {
        realm.Add(new IndexInfoRealm()
        {
            CommitId = commitId.Sha,
            Version = Guid.NewGuid(),
        }, update: true);
    }

    private byte[]? GetEntryData(TreeItem item, bool delete)
    {
        if (delete)
        {
            return null;
        }
        else if (item is Resource resource)
        {
            return resource.Embedded.GetBytes();
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

    public IndexEntry? TryLoadEntry(DataPath path) => DoRealmAction(realm =>
        TryLoadEntry(path, realm));

    private static IndexEntry? TryLoadEntry(DataPath path, Realm realm) =>
        realm.All<IndexEntry>().FirstOrDefault(e => e.PathAsString.Equals((string?)path.FilePath, StringComparison.Ordinal));

    public TItem? TryLoadItem<TItem>(DataPath path, bool onlyIndex = false)
        where TItem : TreeItem => DoRealmAction(_ =>
        !onlyIndex || TryLoadEntry(path) is not null ?
        (TItem?)_loader.Execute(_connection, new(GetAndVerifyBranchTip().Tree, this, path)) :
        null);

    public TreeItem LoadItem(IndexEntry entry) => DoRealmAction(_ =>
        _loader.Execute(_connection, new(GetAndVerifyBranchTip().Tree, this, entry.Path!)) ??
        throw new GitObjectDbException($"The entry for path {entry.Path} does not exist."));

    public void DoRealmAction(Action<Realm> action) => DoRealmAction<object?>(param =>
    {
        action(param);
        return null;
    });

    public TResult DoRealmAction<TResult>(Func<Realm, TResult> func)
    {
        _realm ??= Realm.GetInstance(IndexStoragePath);
        _nestedCount++;
        try
        {
            return func(_realm);
        }
        finally
        {
            _nestedCount--;

            if ( _nestedCount == 0 )
            {
                // Disposes the current instance and closes the native Realm if this is the last remaining
                // instance holding a reference to it.
                _realm.Dispose();
                _realm = null;
            }
        }
    }

    internal partial class IndexInfoRealm : IRealmObject
    {
        [PrimaryKey]
        public string Id { get; private set; } = "$info";

        public string? CommitId { get; set; }

        public Guid Version { get; set; }
    }
}
