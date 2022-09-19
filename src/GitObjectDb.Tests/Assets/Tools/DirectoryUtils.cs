using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Assets.Tools;

/// <summary>
/// Set of tools for managing directories.
/// </summary>
internal static class DirectoryUtils
{
    /// <summary>
    /// Deletes the specified target dir and all its children recursively.
    /// </summary>
    /// <param name="targetDir">The target dir.</param>
    /// <param name="continueOnError">if set to <c>true</c> ignore errors.</param>
    /// <param name="exclusionList">File/directory exclusion list.</param>
    /// <returns><code>true</code> if all files and directories could be deleted. <code>false</code> otherwise.</returns>
    internal static bool Delete(string targetDir, bool continueOnError, params string[] exclusionList)
    {
        var fixedExclusionList = exclusionList.Select(p => System.IO.Path.GetFullPath(p)).ToArray();
        return DeleteImpl(targetDir, continueOnError, fixedExclusionList);
    }

    private static bool DeleteImpl(string targetDir, bool continueOnError, params string[] fixedExclusionList)
    {
        if (!Directory.Exists(targetDir))
        {
            return true;
        }
        if (fixedExclusionList.Contains(System.IO.Path.GetFullPath(targetDir)))
        {
            return false;
        }

        var result = true;
        try
        {
            result &= DeleteNestedFiles(targetDir, continueOnError, fixedExclusionList);
            result &= DeleteNestedDirectories(targetDir, continueOnError, fixedExclusionList);

            if (result)
            {
                File.SetAttributes(targetDir, FileAttributes.Normal);
                Directory.Delete(targetDir, false);
            }
        }
        catch
        {
            if (!continueOnError)
            {
                throw;
            }
            result = false;
        }
        return result;
    }

    private static bool DeleteNestedFiles(string targetDir, bool continueOnError, string[] fixedExclusionList)
    {
        var result = true;
        var files = Directory.GetFiles(targetDir);
        foreach (var file in files)
        {
            if (fixedExclusionList.Contains(file))
            {
                continue;
            }
            try
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            catch
            {
                if (!continueOnError)
                {
                    throw;
                }
                result = false;
            }
        }
        return result;
    }

    private static bool DeleteNestedDirectories(string targetDir, bool continueOnError, string[] fixedExclusionList)
    {
        var result = true;
        try
        {
            var dirs = Directory.GetDirectories(targetDir);
            foreach (var dir in dirs)
            {
                result &= DeleteImpl(dir, continueOnError, fixedExclusionList);
            }
        }
        catch
        {
            if (!continueOnError)
            {
                throw;
            }
            result = false;
        }
        return result;
    }
}
