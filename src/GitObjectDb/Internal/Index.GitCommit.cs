using GitObjectDb.Internal.Commands;
using LibGit2Sharp;
using Realms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using static GitObjectDb.Internal.Commands.CommitCommand;
using static GitObjectDb.Internal.Commands.GitUpdateCommand;

namespace GitObjectDb.Internal;
internal partial class Index
{
    private readonly ICommitCommand _commitCommand;

    public Commit Commit(CommitDescription description)
    {
        var predecessor = GetAndVerifyBranchTip();
        var parents = GetParents(description, predecessor);
        var result = DoRealmAction(realm => Commit(description, realm, predecessor, parents));
        Reset();
        return result;
    }

    private Commit Commit(CommitDescription description, Realm realm, Commit predecessor, List<Commit> parents)
    {
        var tree = predecessor.Tree;
        var modules = GetModuleFromIndexOrTree(realm, tree);
        var result = _commitCommand.Commit(_connection,
                                           i => ApplyEntries(i, realm, predecessor.Tree, modules),
                                           BranchName,
                                           parents,
                                           description);
        return result;
    }

    private static ModuleCommands GetModuleFromIndexOrTree(Realm realm, Tree tree)
    {
        var entry = realm.All<IndexEntry>()
            .FirstOrDefault(e => e.PathAsString.Equals(ModuleCommands.ModuleFile, StringComparison.Ordinal));
        return entry?.Data is not null ?
            new ModuleCommands(new MemoryStream(entry.Data)) :
            new ModuleCommands(tree);
    }

    private void ApplyEntries(ImportFileArguments info, Realm realm, Tree tree, ModuleCommands modules)
    {
        foreach (var entry in realm.All<IndexEntry>())
        {
            ApplyEntry(entry, tree, modules, info.Writer, info.Index);
        }

        if (modules.HasAnyChange)
        {
            using var stream = modules.CreateStream();
            AddBlob(ModuleCommands.ModuleFile, stream, info.Writer, info.Index);
        }
    }

    private void ApplyEntry(IndexEntry entry,
                            Tree tree,
                            ModuleCommands modules,
                            StreamWriter writer,
                            IList<string> index)
    {
        if (entry.Path is null)
        {
            return;
        }
        if (entry.Delete)
        {
            var action = GitUpdateCommand.Delete(entry.Path, _connection.Serializer);
            action.Invoke(tree, modules, _connection.Serializer, writer, index);
        }
        else
        {
            using var stream = new MemoryStream(entry.Data!);
            AddBlob(entry.Path!.FilePath, stream, writer, index);
            CreateOrUpdatePropertiesStoredAsSeparateFiles(entry, writer, index);
        }
        if (entry.Path.IsNode(_connection.Serializer))
        {
            var link = entry.RemoteResourceRepository is not null && entry.RemoteResourceSha is not null ?
                new ResourceLink(entry.RemoteResourceRepository, entry.RemoteResourceSha) : null;
            CreateOrUpdateNodeRemoteResource(entry.Path, link, tree, index, modules);
        }
    }

    private void CreateOrUpdatePropertiesStoredAsSeparateFiles(IndexEntry entry,
        StreamWriter writer,
        ICollection<string> commitIndex)
    {
        var type = Type.GetType(entry.Type!);
        if (type is null || !typeof(Node).IsAssignableFrom(type))
        {
            return;
        }
        foreach (var info in _connection.Model.GetDescription(type).StoredAsSeparateFilesProperties)
        {
            var path = new DataPath(entry.Path!.FolderPath,
                $"{Path.GetFileNameWithoutExtension(entry.Path!.FileName)}.{info.Property.Name}.{info.Extension}",
                false);
            if (!entry.ExternalPropertyValues.TryGetValue(info.Property.Name, out var value))
            {
                GitUpdateCommand.Delete(path, _connection.Serializer);
            }
            else
            {
                AddBlob(path, new MemoryStream(Encoding.Default.GetBytes(value)), writer, commitIndex);
            }
        }
    }
}
