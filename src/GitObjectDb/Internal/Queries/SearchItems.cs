using Fasterflect;
using GitObjectDb.Model;
using GitObjectDb.Tools;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Internal.Queries;

internal class SearchItems : IQuery<SearchItems.Parameters, IEnumerable<(DataPath Path, TreeItem Item)>>
{
    private readonly IQuery<LoadItem.Parameters, TreeItem?> _loader;

    public SearchItems(IQuery<LoadItem.Parameters, TreeItem?> loader)
    {
        _loader = loader;
    }

    public IEnumerable<(DataPath Path, TreeItem Item)> Execute(IQueryAccessor queryAccessor, Parameters parms)
    {
        var regex = queryAccessor.Serializer.EscapeRegExPattern(parms.Pattern);
        var arguments = $"grep --name-only " +
            $"{(parms.IgnoreCase ? "--ignore-case " : string.Empty)}" +
            $"{(parms.RecurseSubModules ? "--recurse-submodules " : string.Empty)}" +
            $"--extended-regexp \"{regex.Replace("\"", "\"\"")}\" " +
            $"{parms.Committish} -- " +
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
                        let item = new Lazy<TreeItem>(() => _loader.Execute(queryAccessor, new LoadItem.Parameters(parms.Tree, Index: null, path!))!)
                        select (path, item);
        return lazyItems
            .AsParallel()
                .Select(i => (i.path, i.item.Value))
                .Where(i => Matches(i.Value, queryAccessor.Model, parms))
                .OrderBy(i => i.path)
            .AsSequential();
    }

    private static bool Matches(TreeItem item, IDataModel model, Parameters parms)
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

    internal record struct Parameters(IConnection Connection,
                                      Tree Tree,
                                      string Pattern,
                                      DataPath? ParentPath,
                                      string Committish,
                                      bool IgnoreCase,
                                      bool RecurseSubModules);
}
