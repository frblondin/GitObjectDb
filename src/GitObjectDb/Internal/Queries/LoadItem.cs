using Fasterflect;
using GitDotNet;
using GitObjectDb.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Internal.Queries;

internal class LoadItem(IDataModel model, INodeSerializer serializer)
    : IAsyncQuery<LoadItem.Parameters, TreeItem?>
{
    private const long ItemSizeInCache = 1L;

    public async Task<TreeItem?> ExecuteAsync(IConnection queryAccessor, Parameters parms)
    {
        return await TryLoadFromIndexAsync(queryAccessor, parms, LoadAsync).ConfigureAwait(false) ??
               await LoadFromTreeAsync(queryAccessor, parms, LoadAsync).ConfigureAwait(false);

        async Task<TreeItem?> LoadAsync(ICacheEntry entry, Func<Task<EntryData>>? streamProvider)
        {
            UpdateCacheEntryExpiration(entry);

            if (streamProvider is null)
            {
                return null;
            }

            if (parms.Path.IsNode(serializer))
            {
                return await LoadNodeAsync(queryAccessor, parms, streamProvider).ConfigureAwait(false);
            }
            else
            {
                return LoadResource(parms, new(async () => (await streamProvider.Invoke().ConfigureAwait(false)).Stream));
            }
        }

        void UpdateCacheEntryExpiration(ICacheEntry entry) =>
            entry.SetSlidingExpiration(parms.Index is not null ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(60));
    }

    private async Task<TreeItem?> TryLoadFromIndexAsync(IConnection queryAccessor, Parameters parms, Func<ICacheEntry, Func<Task<EntryData>>?, Task<TreeItem?>> loader)
    {
        if (parms.Index?.Version != null)
        {
            var indexEntry = parms.Index.TryLoadEntry(parms.Path);
            if (indexEntry?.Data is not null)
            {
                var loadParameters = new DataLoadFromIndexParameters(parms.Path, parms.Index.Version.Value);
                return await GetOrCreateInCacheAsync(
                    queryAccessor.Cache,
                    loadParameters,
                    loader,
                    GetContent).ConfigureAwait(false);
                Task<EntryData> GetContent() =>
                    Task.FromResult(new EntryData(new MemoryStream(indexEntry.Data!), indexEntry.ExternalPropertyValues));
            }
        }
        return null;
    }

    private async Task<TreeItem?> LoadFromTreeAsync(IConnection queryAccessor, Parameters parms, Func<ICacheEntry, Func<Task<EntryData>>?, Task<TreeItem?>> loader)
    {
        var loadParameters = new DataLoadFromTreeParameters(parms.Path, parms.Tree.Id, parms.Index?.Version);
        var filePath = parms.Path.FilePath;
        var treeEntry = await parms.Tree.GetFromPathAsync(filePath).ConfigureAwait(false);
        return await GetOrCreateInCacheAsync(
            queryAccessor.Cache,
            loadParameters,
            treeEntry is null ? null : loader,
            GetContentAsync).ConfigureAwait(false);

        async Task<EntryData> GetContentAsync()
        {
            var blob = await treeEntry!.GetEntryAsync<BlobEntry>().ConfigureAwait(false);
            var prefix = $"{Path.GetFileNameWithoutExtension(parms.Path.FileName)}.";
            var parentFolder = await parms.Tree.GetFromPathAsync(parms.Path.FolderPath).ConfigureAwait(false);
            var parentFolderTree = await parentFolder!.GetEntryAsync<TreeEntry>().ConfigureAwait(false);
            var chilren = parentFolderTree.Children
                .Where(f => f.Mode.Type == ObjectType.RegularFile &&
                       f.Name.StartsWith(prefix, StringComparison.Ordinal) &&
                       f.Name.Count(c => c == '.') > 1);
            return new(blob.OpenRead(), await ConvertToDictionaryAsync(chilren).ConfigureAwait(false));
        }
    }

    private static async Task<IDictionary<string, string>> ConvertToDictionaryAsync(
        IEnumerable<TreeEntryItem> entries)
    {
        var result = new Dictionary<string, string>();
        foreach (var entry in entries)
        {
            var index = entry.Name.IndexOf('.');
            var name = Path.GetFileNameWithoutExtension(entry.Name[(index + 1)..]);
            var blob = await entry.GetEntryAsync<BlobEntry>().ConfigureAwait(false);
            result[name] = blob.GetText()!;
        }
        return result;
    }

    private static async Task<TreeItem?> GetOrCreateInCacheAsync(IMemoryCache cache,
        object key,
        Func<ICacheEntry, Func<Task<EntryData>>?, Task<TreeItem?>>? loader,
        Func<Task<EntryData>>? content)
    {
        if (!cache.TryGetValue(key, out var result))
        {
            using var entry = cache.CreateEntry(key);
            entry.SetSize(ItemSizeInCache);
            result = entry.Value = loader != null ? await loader!.Invoke(entry, content).ConfigureAwait(false) : null;
        }

        return (TreeItem?)result;
    }

    private async Task<TreeItem> LoadNodeAsync(IConnection queryAccessor, Parameters parms, Func<Task<EntryData>> streamProvider)
    {
        var data = await streamProvider.Invoke().ConfigureAwait(false);
        using var stream = data.Stream;
        var result = await queryAccessor.Serializer.DeserializeAsync(stream,
            parms.Tree.Id,
            parms.Path,
            async p => await ExecuteAsync(queryAccessor, parms with { Path = p }).ConfigureAwait(false) ??
                 throw new GitObjectDbException($"The entry for path {p} does not exist."));

        foreach (var property in model.GetDescription(result.GetType()).StoredAsSeparateFilesProperties
                     .Select(info => info.Property))
        {
            if (data.PropertyStoredAsFileValues?.TryGetValue(property.Name, out var value) ?? false)
            {
                Reflect.PropertySetter(property).Invoke(result, value);
            }
        }

        return result;
    }

    private static TreeItem LoadResource(Parameters parms, Resource.Data data) =>
        new Resource(parms.Path, data);

    internal record struct Parameters(TreeEntry Tree, IIndex? Index, DataPath Path);

    private record struct DataLoadFromTreeParameters(DataPath Path, HashId TreeId, Guid? IndexVersion);

    private record struct DataLoadFromIndexParameters(DataPath Path, Guid Guid);

    private record struct EntryData(Stream Stream, IDictionary<string, string>? PropertyStoredAsFileValues);
}
