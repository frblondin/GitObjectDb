using Nuke.Common.Tools.Git;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal static class GitChangeLogTasks
{
    public static IEnumerable<string> ChangedFilesSinceLastTag()
    {
        var lastTag = GitTasks
            .Git("describe --tags --abbrev=0")
            .Select(x => x.Text)
            .FirstOrDefault();
        Serilog.Log.Information("Found most recent tag '{LastTag}'", lastTag);

        var result = lastTag != null ? GitTasks
            .Git($"diff --name-only {lastTag}..HEAD")
            .Select(x => x.Text)
            .ToList() : null;
        Serilog.Log.Information("Found {ModifiedFilesCount} changes since last tag", result?.Count);

        return result ?? Enumerable.Empty<string>();
    }

    public static IEnumerable<string> CommitsSinceLastTag()
    {
        var lastTag = GitTasks
            .Git("describe --tags --abbrev=0")
            .Select(x => x.Text)
            .FirstOrDefault();
        Serilog.Log.Information("Found most recent tag '{LastTag}'", lastTag);

        var result = lastTag != null ? GitTasks
            .Git($"log --pretty=format:%s {lastTag}..HEAD")
            .Select(x => x.Text)
            .ToList() : null;
        Serilog.Log.Information("Found {ModifiedFilesCount} changes since last tag", result?.Count);

        return result;
    }

    public static IEnumerable<string> GetModifiedLinesSinceLastTag(string path)
    {
        var lastTag = GitTasks
            .Git("describe --tags --abbrev=0")
            .Select(x => x.Text)
            .FirstOrDefault();
        Serilog.Log.Information("Found most recent tag '{LastTag}'", lastTag);

        var result = lastTag != null ? GitTasks
            .Git($"diff {lastTag}..HEAD -- {path}")
            .Where(x => x.Text.StartsWith("+"))
            .Select(x => x.Text.Substring(1).Trim())
            .ToList() : null;
        Serilog.Log.Information("Found {ModifiedLinesCount} changes since last tag in file {path}", result?.Count, path);

        return result ?? Enumerable.Empty<string>();
    }
}
