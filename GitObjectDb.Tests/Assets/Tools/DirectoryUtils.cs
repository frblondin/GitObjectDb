using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitObjectDb.Tests.Assets.Tools
{
    public static class DirectoryUtils
    {
        public static void Delete(string targetDir)
        {
            if (!Directory.Exists(targetDir))
            {
                return;
            }

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
                Delete(dir);
            }

            Directory.Delete(targetDir, false);
        }
    }
}
