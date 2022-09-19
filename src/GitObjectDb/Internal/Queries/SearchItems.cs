using Fasterflect;
using GitObjectDb.Model;
using GitObjectDb.Tools;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GitObjectDb.Internal.Queries;

internal class SearchItems : IQuery<SearchItems.Parameters, IEnumerable<(DataPath Path, ITreeItem Item)>>
{
    private readonly IQuery<LoadItem.Parameters, ITreeItem> _loader;
    private readonly RecyclableMemoryStreamManager _streamManager;

    public SearchItems(IQuery<LoadItem.Parameters, ITreeItem> loader, RecyclableMemoryStreamManager streamManager)
    {
        _loader = loader;
        _streamManager = streamManager;
    }

    public IEnumerable<(DataPath Path, ITreeItem Item)> Execute(IQueryAccessor queryAccessor, Parameters parms)
    {
        var regex = queryAccessor.Serializer.EscapeRegExPattern(parms.Pattern);
        var arguments = $"grep --name-only " +
            $"{(parms.IgnoreCase ? "--ignore-case " : string.Empty)}" +
            $"{(parms.RecurseSubModules ? "--recurse-submodules " : string.Empty)}" +
            $"--extended-regexp \"{regex.Replace("\"", "\"\"")}\" " +
            $"{parms.Committish ?? "HEAD"} -- " +
            $"{(parms.ParentPath is not null ? $"'{parms.ParentPath.FolderPath}'" : string.Empty)}";

        var result = new List<string?>();
        GitCliCommand.Execute(parms.Connection.Repository.Info.Path,
                              arguments,
                              throwOnError: false,
                              outputDataReceived: (_, e) => result.Add(e.Data));

        DataPath? path = default;
        var lazyItems = from data in result
                        where data is not null
                        let colon = data.IndexOf(':')
                        where DataPath.TryParse(data.Substring(colon + 1), out path)
                        let item = new Lazy<ITreeItem>(() => _loader.Execute(queryAccessor, new LoadItem.Parameters(parms.Tree, path!, parms.ReferenceCache)))
                        select (path, item);
        return lazyItems
            .AsParallel()
                .Select(i => (i.path, i.item.Value))
                .Where(i => Matches(i.Value, queryAccessor.Model, parms))
                .OrderBy(i => i.path)
            .AsSequential();
    }

    private static bool Matches(ITreeItem item, IDataModel model, Parameters parms)
    {
        var comparer = parms.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return item switch
        {
            Node node => Matches(node, model, parms, comparer),
            Resource resource => Matches(resource, parms, comparer),
            _ => throw new NotSupportedException($"{item.GetType()} is not supported."),
        };
    }

    private static bool Matches(Node node, IDataModel model, Parameters parms, StringComparison comparison)
    {
        var description = model.GetDescription(node.GetType());
        foreach (var property in description.SearchableProperties)
        {
            var getter = Reflect.PropertyGetter(property);
            var value = getter(node);
            if (Matches(value?.ToString(), parms.Pattern, comparison))
            {
                return true;
            }
        }
        return false;
    }

    private static bool Matches(Resource resource, Parameters parms, StringComparison comparison)
    {
        var value = resource.Embedded.ReadAsString();
        return Matches(value, parms.Pattern, comparison);
    }

    private static bool Matches(string? value, string pattern, StringComparison comparison) =>
        value is not null &&
        value.ToString().IndexOf(pattern, comparison) != -1;

    internal record Parameters
    {
        public Parameters(IConnection connection,
                          Tree tree,
                          string pattern,
                          DataPath? parentPath,
                          string? committish,
                          bool ignoreCase,
                          bool recurseSubModules,
                          IMemoryCache referenceCache)
        {
            Connection = connection;
            Tree = tree;
            Pattern = pattern;
            ParentPath = parentPath;
            Committish = committish;
            IgnoreCase = ignoreCase;
            RecurseSubModules = recurseSubModules;
            ReferenceCache = referenceCache;
        }

        public IConnection Connection { get; }

        public Tree Tree { get; }

        public string Pattern { get; }

        public DataPath? ParentPath { get; }

        public string? Committish { get; }

        public bool IgnoreCase { get; }

        public bool RecurseSubModules { get; }

        public IMemoryCache ReferenceCache { get; }
    }
}