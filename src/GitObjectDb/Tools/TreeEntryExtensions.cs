using System.Collections.Generic;
using System.Linq;

namespace GitDotNet;

internal static class TreeEntryExtensions
{
    internal static IAsyncEnumerable<(TreeEntryItem Entry, GitPath Path)> TraverseAsync(this TreeEntryItem entry,
        GitPath path,
        bool includeSelf = true)
    {
        var entries = new Stack<(TreeEntryItem Entry, GitPath Path)>();
        entries.Push((entry, path));
        return AddNestedChildrenAsync(entries).Skip(includeSelf ? 0 : 1);
    }

    internal static IAsyncEnumerable<(TreeEntryItem Entry, GitPath Path)> TraverseAsync(this TreeEntry entry,
        GitPath path)
    {
        var entries = new Stack<(TreeEntryItem Entry, GitPath Path)>();
        foreach (var child in entry.Children)
        {
            entries.Push((child, $"{path}/{child.Name}"));
        }
        return AddNestedChildrenAsync(entries);
    }

    private static async IAsyncEnumerable<(TreeEntryItem Entry, GitPath Path)> AddNestedChildrenAsync(
        Stack<(TreeEntryItem Entry, GitPath Path)> entries)
    {
        while (entries.Count > 0)
        {
            var current = entries.Pop();
            yield return current;

            if (current.Entry.Mode.Type == ObjectType.Tree)
            {
                var tree = await current.Entry.GetEntryAsync<TreeEntry>().ConfigureAwait(false);
                foreach (var child in tree.Children)
                {
                    entries.Push((child, $"{current.Path}/{child.Name}"));
                }
            }
        }
    }
}
