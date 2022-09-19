using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb
{
    /// <summary>
    /// Provides details about special folders.
    /// </summary>
    public static class FileSystemStorage
    {
        /// <summary>
        /// The data file name used to store information in Git.
        /// </summary>
        public const string ResourceFolder = "Resources";

        internal static ISet<string> ReservedNames { get; } = new HashSet<string>(
            new[] { ResourceFolder },
            StringComparer.OrdinalIgnoreCase);

        internal static void ThrowIfAnyReservedName(string path)
        {
            foreach (var reserved in ReservedNames)
            {
                if (path.IndexOf($"/{reserved}/", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    throw new GitObjectDbException("The path contains reserved folder names;");
                }
            }
        }
    }
}
