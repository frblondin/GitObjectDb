using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    /// <summary>
    /// A set of methods for instances of <see cref="string"/>.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Returns the parent path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="ArgumentException">path</exception>
        /// <returns>The parent path.</returns>
        internal static string ParentPath(this string path, int count = 1)
        {
            if (count == 1 && string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var position = path.Length - 1;
            var remaining = count;
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
