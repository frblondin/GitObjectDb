using Fasterflect;
using GitDotNet;
using GitObjectDb.Model;
using GitObjectDb.Tools;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GitObjectDb.Internal.Queries;

internal class SearchItems(IAsyncQuery<LoadItem.Parameters, TreeItem?> loader)
    : IAsyncEnumerableQuery<SearchItems.Parameters, (DataPath Path, TreeItem Item)>
{
    public async IAsyncEnumerable<(DataPath Path, TreeItem Item)> ExecuteAsync(IConnection queryAccessor,
        Parameters parms, [EnumeratorCancellation] CancellationToken token = default)
    {
        var regex = queryAccessor.Serializer.EscapeRegExPattern(parms.Pattern);
        var arguments = $"grep --name-only " +
            $"{(parms.IgnoreCase ? "--ignore-case " : string.Empty)}" +
            $"{(parms.RecurseSubModules ? "--recurse-submodules " : string.Empty)}" +
            $"--extended-regexp \"{regex.Replace("\"", "\"\"")}\" " +
            $"{parms.Commit.Id} -- " +
            $"{(parms.ParentPath is not null ? $"'{parms.ParentPath.FolderPath}'" : string.Empty)}";

        var channel = Channel.CreateUnbounded<string>();
        var task = Task.Run(() =>
        {
            GitCliCommand.Execute(parms.Connection.Repository.Info.Path,
                arguments,
                throwOnError: false,
                outputDataReceived: (_, e) =>
                {
                    if (e.Data != null)
                    {
                        channel.Writer.TryWrite(e.Data!);
                    }
                });
            channel.Writer.Complete();
        }, token).ConfigureAwait(false);
        await foreach (var data in channel.Reader.ReadAllAsync(token).ConfigureAwait(false))
        {
            var colon = data?.IndexOf(':') ?? -1;
            if (colon != -1 &&
                DataPath.TryParse(data![(colon + 1)..], out var path))
            {
                var item = await loader.ExecuteAsync(queryAccessor, new LoadItem.Parameters(parms.Tree, Index: null, path!)).ConfigureAwait(false)!;
                if (item != null && await MatchesAsync(item, queryAccessor.Model, parms).ConfigureAwait(false))
                {
                    yield return (path!, item);
                }
            }
        }
        await task;
    }

    private static async Task<bool> MatchesAsync(TreeItem item, IDataModel model, Parameters parms)
    {
        var comparer = parms.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return item switch
        {
            Node node => Matches(node, model, parms, comparer),
            Resource resource => await MatchesAsync(resource, parms, comparer).ConfigureAwait(false),
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

    private static async Task<bool> MatchesAsync(Resource resource, Parameters parms, StringComparison comparison)
    {
        var value = await resource.Embedded.ReadAsStringAsync().ConfigureAwait(false);
        return Matches(value, parms.Pattern, comparison);
    }

    private static bool Matches(string? value, string pattern, StringComparison comparison) =>
        value?.Contains(pattern, comparison) ?? false;

    internal record struct Parameters(IConnection Connection,
        TreeEntry Tree,
        string Pattern,
        DataPath? ParentPath,
        CommitEntry Commit,
        bool IgnoreCase,
        bool RecurseSubModules);
}
