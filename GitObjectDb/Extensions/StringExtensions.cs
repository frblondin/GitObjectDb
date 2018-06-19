using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    internal static class StringExtensions
    {
        internal static string ParentPath(this string path, int count = 1)
        {
            if (count == 1 && string.IsNullOrEmpty(path)) return "";
            var position = path.Length - 1;
            var remaining = count;
            var result = path;
            while (remaining-- > 0)
            {
                position = path.LastIndexOf('/', position);
                if (position == -1)
                {
                    throw new ArgumentException($"The parent path could not be found for '{path}'.", nameof(path));
                }
            }
            return path.Substring(0, position);
        }
    }
}
