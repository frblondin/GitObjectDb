using System.Collections.Generic;

namespace LibGit2Sharp;

internal static class TreeEntryExtensions
{
    internal static IEnumerable<(TreeEntry Entry, string Path)> Traverse(this TreeEntry entry, string path)
    {
        var entries = new Stack<(TreeEntry Entry, string Path)>();
        entries.Push((entry, path));
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
