using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GitObjectDb
{
    /// <summary>Provides details about special folders.</summary>
    public static class FileSystemStorage
    {
        /// <summary>The data file name used to store information in Git.</summary>
        public const string ResourceFolder = "Resources";

        private static Regex _reservedNames = new(
            $"(^|/)({ResourceFolder})($|/)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        internal static void ThrowIfAnyReservedName(string path)
        {
            if (_reservedNames.IsMatch(path))
            {
                throw new GitObjectDbException("The path contains reserved folder names;");
            }
        }
    }
}
