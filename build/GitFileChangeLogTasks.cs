using Nuke.Common.Tools.Git;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal static class GitFileChangeLogTasks
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

        return result;
    }
}
