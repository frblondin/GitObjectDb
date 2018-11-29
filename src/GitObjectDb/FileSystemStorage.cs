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
        public const string DataFile = "data.json";

        /// <summary>
        /// The migration folder.
        /// </summary>
        public const string MigrationFolder = "$Migrations";

        /// <summary>
        /// The prefix of special folders.
        /// </summary>
        public const char SpecialFolderPrefix = '$';

        /// <summary>
        /// Gets the git path containing migrations.
        /// </summary>
        public static string Migrations { get; } = $"{SpecialFolderPrefix}Migrations";
    }
}
