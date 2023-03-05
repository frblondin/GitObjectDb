using GitObjectDb.Internal.Commands;
using LibGit2Sharp;
using Realms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        return DoRealmAction(realm => Commit(description, realm, predecessor, parents));
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
        Reset();
        return result;
    }

    private static ModuleCommands GetModuleFromIndexOrTree(Realm realm, Tree tree)
    {
        var entry = realm.All<IndexEntry>()
            .FirstOrDefault(e => e.PathAsString.Equals(ModuleCommands.ModuleFile, System.StringComparison.Ordinal));
        return entry is not null ?
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
                            List<string> index)
    {
        if (entry.Path is null)
        {
            return;
        }
        if (entry.Delete)
        {
            var action = GitUpdateCommand.Delete(entry.Path);
            action.Invoke(tree, modules, _connection.Serializer, writer, index);
        }
        else
        {
            using var stream = new MemoryStream(entry.Data);
            AddBlob(entry.Path!.FilePath, stream, writer, index);
        }
        if (entry.Path.IsNode)
        {
            var link = entry.RemoteResourceRepository is not null && entry.RemoteResourceSha is not null ?
                new ResourceLink(entry.RemoteResourceRepository, entry.RemoteResourceSha) : null;
            CreateOrUpdateNodeRemoteResource(entry.Path, link, tree, index, modules);
        }
    }
}
