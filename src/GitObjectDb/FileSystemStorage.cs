using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GitObjectDb;

/// <summary>Provides details about special folders.</summary>
public static class FileSystemStorage
{
    private static readonly Regex _resourcePath;

    static FileSystemStorage()
    {
        ResourceFolder = "Resources";
        _resourcePath = new($"(^|/)({ResourceFolder})($|/)",
                            RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    /// <summary>Gets the data file name used to store information in Git.</summary>
    public static string ResourceFolder { get; }

    internal static void ThrowIfAnyReservedName(string path)
    {
        if (IsResourcePath(path))
        {
            throw new GitObjectDbException("The path contains reserved folder names;");
        }
    }

    internal static bool IsResourcePath(string path) =>
        _resourcePath.IsMatch(path);

    internal static bool IsResourceName(string name) =>
        name.Equals(ResourceFolder, StringComparison.Ordinal);
}
