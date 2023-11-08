using Fasterflect;
using GitObjectDb.Model;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitObjectDb.Internal.Queries;

internal class LoadItem : IQuery<LoadItem.Parameters, TreeItem?>
{
    private static readonly object _referenceLock = new();
    private readonly IDataModel _model;
    private readonly INodeSerializer _serializer;

    public LoadItem(IDataModel model, INodeSerializer serializer)
    {
        _model = model;
        _serializer = serializer;
    }

    public TreeItem? Execute(IQueryAccessor queryAccessor, Parameters parms)
    {
        return TryLoadFromIndex(queryAccessor, parms, Load) ??
               LoadFromTree(queryAccessor, parms, Load);

        TreeItem? Load(ICacheEntry entry, Func<EntryData>? streamProvider)
        {
            UpdateCacheEntryExpiration(entry);

            if (streamProvider is null)
            {
                return null;
            }

            return parms.Path.IsNode(_serializer) ?
                LoadNode(queryAccessor, parms, streamProvider) :
                LoadResource(parms, new(() => streamProvider.Invoke().Stream));
        }

        void UpdateCacheEntryExpiration(ICacheEntry entry) =>
            entry.SetSlidingExpiration(parms.Index is not null ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(60));
    }

    private TreeItem? TryLoadFromIndex(IQueryAccessor queryAccessor, Parameters parms, Func<ICacheEntry, Func<EntryData>?, TreeItem?> loader)
    {
        if (parms.Index?.Version != null)
        {
            var indexEntry = parms.Index.TryLoadEntry(parms.Path);
            if (indexEntry?.Data is not null)
            {
                var loadParameters = new DataLoadFromIndexParameters(parms.Path, parms.Index.Version.Value);
                return GetOrCreateInCacheWithNoRacingCondition(
                    queryAccessor.Cache,
                    loadParameters,
                    loader,
                    GetContent);
                EntryData GetContent() =>
                    new(new MemoryStream(indexEntry.Data), indexEntry.ExternalPropertyValues);
            }
        }
        return null;
    }

    private TreeItem? LoadFromTree(IQueryAccessor queryAccessor, Parameters parms, Func<ICacheEntry, Func<EntryData>?, TreeItem?> loader)
    {
        var loadParameters = new DataLoadFromTreeParameters(parms.Path, parms.Tree.Id, parms.Index?.Version);
        var filePath = parms.Path.FilePath;
        var treeEntry = parms.Tree[filePath];
        return GetOrCreateInCacheWithNoRacingCondition(
            queryAccessor.Cache,
            loadParameters,
            treeEntry is null ? null : loader,
            GetContent);

        EntryData GetContent()
        {
            var blob = treeEntry!.Target.Peel<Blob>();
            var prefix = $"{Path.GetFileNameWithoutExtension(parms.Path.FileName)}.";
            var propertyStoredAsSeparateFileValues = parms.Tree[parms.Path.FolderPath].Target.Peel<Tree>()
                .Where(f => f.TargetType == TreeEntryTargetType.Blob &&
                            f.Name.StartsWith(prefix, StringComparison.Ordinal) &&
                            f.Name.Count(c => c == '.') > 1)
                .ToDictionary(ExtractPropertyName, ExtractPropertyValue);
            return new(blob.GetContentStream(), propertyStoredAsSeparateFileValues);

            static string ExtractPropertyName(TreeEntry entry)
            {
                var index = entry.Name.IndexOf('.');
                return Path.GetFileNameWithoutExtension(entry.Name.Substring(index + 1));
            }
            static string ExtractPropertyValue(TreeEntry entry)
            {
                return entry.Target.Peel<Blob>().GetContentText();
            }
        }
    }

    /// <summary>
    /// IMemoryCache doesn't protect against racing conditions, that is when two threads are calling GetOrCreate for
    /// the same key. In this case, the factory can be called simultaneously and two different instances can be returned.
    /// This method uses a reentrant locking mechanism.
    /// </summary>
    private TreeItem? GetOrCreateInCacheWithNoRacingCondition(IMemoryCache cache,
        object key,
        Func<ICacheEntry, Func<EntryData>?, TreeItem?>? loader,
        Func<EntryData>? content)
    {
        if (!cache.TryGetValue(key, out var result))
        {
            lock (_referenceLock)
            {
                if (!cache.TryGetValue(key, out result))
                {
                    using var entry = cache.CreateEntry(key);
                    result = entry.Value = loader?.Invoke(entry, content);
                }
            }
        }

        return (TreeItem?)result;
    }

    private TreeItem LoadNode(IQueryAccessor queryAccessor, Parameters parms, Func<EntryData> streamProvider)
    {
        var data = streamProvider.Invoke();
        using var stream = data.Stream;
        var result = queryAccessor.Serializer.Deserialize(stream,
            parms.Tree.Id,
            parms.Path,
            p => Execute(queryAccessor, parms with { Path = p }) ??
                 throw new GitObjectDbException($"The entry for path {p} does not exist."));

        foreach (var property in _model.GetDescription(result.GetType()).StoredAsSeparateFilesProperties
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

    internal record struct Parameters(Tree Tree, IIndex? Index, DataPath Path);

    private record struct DataLoadFromTreeParameters(DataPath Path, ObjectId TreeId, Guid? IndexVersion);

    private record struct DataLoadFromIndexParameters(DataPath Path, Guid Guid);

    private record struct EntryData(Stream Stream, IDictionary<string, string>? PropertyStoredAsFileValues);
}
