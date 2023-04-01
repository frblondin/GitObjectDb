using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.IO;

namespace GitObjectDb.Internal.Queries;

internal class LoadItem : IQuery<LoadItem.Parameters, TreeItem?>
{
    public TreeItem? Execute(IQueryAccessor queryAccessor, Parameters parms)
    {
        return TryLoadFromIndex(queryAccessor, parms, Load) ??
               LoadFromTree(queryAccessor, parms, Load);

        TreeItem? Load(ICacheEntry entry, Func<Stream>? streamProvider)
        {
            UpdateCacheEntryExpiration(entry);

            if (streamProvider is null)
            {
                return null;
            }

            return parms.Path.IsNode ?
                LoadNode(queryAccessor, parms, streamProvider) :
                LoadResource(parms, new(streamProvider));
        }

        void UpdateCacheEntryExpiration(ICacheEntry entry) =>
            entry.SetSlidingExpiration(parms.Index is not null ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(60));
    }

    private TreeItem? TryLoadFromIndex(IQueryAccessor queryAccessor, Parameters parms, Func<ICacheEntry, Func<Stream>?, TreeItem?> loader)
    {
        if (parms.Index is not null && parms.Index.Version.HasValue)
        {
            var indexEntry = parms.Index.TryLoadEntry(parms.Path);
            if (indexEntry is not null)
            {
                var loadParameters = new DataLoadFromIndexParameters(parms.Path, parms.Index.Version.Value);
                return queryAccessor.Cache.GetOrCreate(loadParameters, entry => loader(entry, GetContent));

                Stream GetContent() => new MemoryStream(indexEntry.Data);
            }
        }
        return null;
    }

    private TreeItem LoadFromTree(IQueryAccessor queryAccessor, Parameters parms, Func<ICacheEntry, Func<Stream>?, TreeItem?> loader)
    {
        var loadParameters = new DataLoadFromTreeParameters(parms.Path, parms.Tree.Id, parms.Index?.Version);
        var filePath = parms.Path.FilePath;
        var treeEntry = parms.Tree[filePath];
        return queryAccessor.Cache.GetOrCreate(loadParameters,
                                               entry => treeEntry is null ? null : loader(entry, GetContent))!;

        Stream GetContent()
        {
            var blob = treeEntry.Target.Peel<Blob>();
            return blob.GetContentStream();
        }
    }

    private TreeItem? LoadNode(IQueryAccessor queryAccessor, Parameters parms, Func<Stream> streamProvider)
    {
        using var stream = streamProvider();
        return stream is null ?
            null :
            queryAccessor.Serializer.Deserialize(stream,
                                                 parms.Tree.Id,
                                                 parms.Path,
                                                 p => Execute(queryAccessor, parms with { Path = p }) ??
                                                    throw new GitObjectDbException($"The entry for path {p} does not exist."));
    }

    private static TreeItem LoadResource(Parameters parms, Resource.Data data) =>
        new Resource(parms.Path, data);

    internal record struct Parameters(Tree Tree, IIndex? Index, DataPath Path);

    private record struct DataLoadFromTreeParameters(DataPath Path, ObjectId TreeId, Guid? IndexVersion);

    private record struct DataLoadFromIndexParameters(DataPath Path, Guid Guid);
}
