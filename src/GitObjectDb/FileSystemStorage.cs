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
        public const string MigrationFolder = SpecialFolderPrefix + "Migrations";

        /// <summary>
        /// The prefix of special folders.
        /// </summary>
        public const string SpecialFolderPrefix = "$";
    }
}
