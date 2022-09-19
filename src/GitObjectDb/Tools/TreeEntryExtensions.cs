using System.Collections.Generic;
using System.Linq;

namespace LibGit2Sharp;

internal static class TreeEntryExtensions
{
    internal static IEnumerable<(TreeEntry Entry, string Path)> Traverse(this TreeEntry entry,
                                                                         string path,
                                                                         bool includeSelf = true)
    {
        var entries = new Stack<(TreeEntry Entry, string Path)>();
        entries.Push((entry, path));
        return AddNestedChildren(entries).Skip(includeSelf ? 0 : 1);
    }

    internal static IEnumerable<(TreeEntry Entry, string Path)> Traverse(this Tree entry,
                                                                         string path)
    {
        var entries = new Stack<(TreeEntry Entry, string Path)>();
        foreach (var child in entry)
        {
            entries.Push((child, $"{path}/{child.Name}"));
        }
        return AddNestedChildren(entries);
    }

    private static IEnumerable<(TreeEntry Entry, string Path)> AddNestedChildren(Stack<(TreeEntry Entry, string Path)> entries)
    {
        while (entries.Count > 0)
        {
            var current = entries.Pop();
            yield return current;

            if (current.Entry.TargetType == TreeEntryTargetType.Tree)
            {
                foreach (var child in current.Entry.Target.Peel<Tree>())
                {
                    entries.Push((child, $"{current.Path}/{child.Name}"));
                }
            }
        }
    }
}
