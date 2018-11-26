using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.ModelCodeGeneration.Tests.Tools
{
    internal class PathTools
    {
        internal static string FindParentDirectoryFile(DirectoryInfo directory, string pattern)
        {
            var solution = directory.GetFiles(pattern).FirstOrDefault();
            if (solution != null)
            {
                return solution.FullName;
            }
            else
            {
                var parent = directory.Parent;
                if (parent == null)
                {
                    throw new NotSupportedException("No solution could be found.");
                }
                return FindParentDirectoryFile(parent, pattern);
            }
        }
    }
}
