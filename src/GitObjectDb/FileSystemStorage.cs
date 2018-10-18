using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb
{
    /// <summary>
    /// Provides details about special folders.
    /// </summary>
    internal static class FileSystemStorage
    {
        /// <summary>
        /// The data file name used to store information in Git.
        /// </summary>
        internal const string DataFile = "data.json";

        /// <summary>
        /// The prefix of special folders.
        /// </summary>
        internal const char Prefix = '$';

        /// <summary>
        /// Gets the git path containing migrations.
        /// </summary>
        internal static string Migrations { get; } = $"{Prefix}Migrations";
    }
}
