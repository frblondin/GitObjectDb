using GitDotNet;
using GitObjectDb.Internal.Commands;
using Realms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GitObjectDb.Internal.Commands.CommitCommand;
using static GitObjectDb.Internal.Commands.GitUpdateCommand;

namespace GitObjectDb.Internal;
internal partial class Index
{
    private readonly ICommitCommand _commitCommand;

    public async Task<CommitEntry> CommitAsync(CommitDescription description)
    {
        var predecessor = await GetAndVerifyBranchTipAsync().ConfigureAwait(false);
        var parents = GetParents(description, predecessor);
        var result = await CommitAsync(description, predecessor, parents).ConfigureAwait(false);
        Reset();
        return result;
    }

    private async Task<CommitEntry> CommitAsync(CommitDescription description, CommitEntry predecessor, List<CommitEntry> parents)
    {
        var tree = await predecessor.GetRootTreeAsync().ConfigureAwait(false);
        var modules = await GetModuleFromIndexOrTreeAsync(tree).ConfigureAwait(false);
        var result = await _commitCommand.CommitAsync(_connection,
            composer => ApplyEntriesAsync(composer, tree, modules),
            BranchName,
            parents,
            description).ConfigureAwait(false);
        return result;
    }

    private async Task<ModuleCommands> GetModuleFromIndexOrTreeAsync(TreeEntry tree)
    {
        using var realm = Realm.GetInstance(Configuration);
        var entry = realm.All<IndexEntry>().AsEnumerable()
            .FirstOrDefault(e => e.PathAsString.Equals(ModuleCommands.ModuleFile, StringComparison.Ordinal));
        return entry?.Data is not null ?
            new ModuleCommands(new MemoryStream(entry.Data)) :
            await ModuleCommands.GetAsync(tree).ConfigureAwait(false);
    }

    private async Task<ITransformationComposer> ApplyEntriesAsync(
        ITransformationComposer composer, TreeEntry tree, ModuleCommands modules)
    {
        using var realm = Realm.GetInstance(Configuration);
        var allEntries = realm.All<IndexEntry>().AsEnumerable().Select(e => e.Freeze()).ToList();
        foreach (var entry in allEntries)
        {
            await ApplyEntryAsync(entry.Freeze(), tree, modules, composer).ConfigureAwait(false);
        }

        if (modules.HasAnyChange)
        {
            var stream = modules.CreateStream();
            composer.AddOrUpdate(ModuleCommands.ModuleFile, stream);
        }
        return composer;
    }

    private async Task ApplyEntryAsync(IndexEntry entry,
        TreeEntry tree,
        ModuleCommands modules,
        ITransformationComposer composer)
    {
        if (entry.Path is null)
        {
            return;
        }
        if (entry.Delete)
        {
            var action = GitUpdateCommand.Delete(entry.Path, _connection.Serializer);
            await action.Invoke(tree, modules, _connection.Serializer, composer).ConfigureAwait(false);
        }
        else
        {
            composer.AddOrUpdate(entry.Path!.FilePath, entry.Data!);
            CreateOrUpdatePropertiesStoredAsSeparateFiles(entry, composer);
        }
        if (entry.Path.IsNode(_connection.Serializer))
        {
            var link = entry.RemoteResourceRepository is not null && entry.RemoteResourceSha is not null ?
                new ResourceLink(entry.RemoteResourceRepository, entry.RemoteResourceSha) : null;
            await CreateOrUpdateNodeRemoteResourceAsync(entry.Path, link, tree, composer, modules).ConfigureAwait(false);
        }
    }

    private void CreateOrUpdatePropertiesStoredAsSeparateFiles(IndexEntry entry,
        ITransformationComposer composer)
    {
        var type = Type.GetType(entry.Type);
        if (type is null || !typeof(Node).IsAssignableFrom(type))
        {
            return;
        }
        foreach (var (property, extension) in _connection.Model.GetDescription(type).StoredAsSeparateFilesProperties)
        {
            var path = new DataPath(entry.Path!.FolderPath,
                $"{Path.GetFileNameWithoutExtension(entry.Path!.FileName)}.{property.Name}.{extension}",
                false);
            if (!entry.ExternalPropertyValues.TryGetValue(property.Name, out var value))
            {
                composer.Remove(path.FilePath);
            }
            else
            {
                composer.AddOrUpdate(path.FilePath, Encoding.Default.GetBytes(value));
            }
        }
    }
}
