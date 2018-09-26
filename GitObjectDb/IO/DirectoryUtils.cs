using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitObjectDb.IO
{
    /// <summary>
    /// Set of tools for managing directories.
    /// </summary>
    internal static class DirectoryUtils
    {
        /// <summary>
        /// Deletes the specified target dir and all its children recursively.
        /// </summary>
        /// <param name="targetDir">The target dir.</param>
        /// <param name="continueOnError">if set to <c>true</c> [continue on error].</param>
        internal static void Delete(string targetDir, bool continueOnError)
        {
            if (!Directory.Exists(targetDir))
            {
                return;
            }

            try
            {
                File.SetAttributes(targetDir, FileAttributes.Normal);

                var files = Directory.GetFiles(targetDir);
                foreach (string file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }

                var dirs = Directory.GetDirectories(targetDir);
                foreach (string dir in dirs)
                {
                    Delete(dir, continueOnError);
                }

                Directory.Delete(targetDir, false);
            }
            catch
            {
                if (!continueOnError)
                {
                    throw;
                }
            }
        }
    }
}
