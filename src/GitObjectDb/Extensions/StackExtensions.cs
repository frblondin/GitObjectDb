using GitObjectDb;
using System;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    /// <summary>
    /// A set of methods for instances of <see cref="Stack{T}"/>.
    /// </summary>
    internal static class StackExtensions
    {
        /// <summary>
        /// Gets the path from stacked path elements.
        /// </summary>
        /// <param name="stack">The stack.</param>
        /// <returns>A <see cref="string"/> value containing the path.</returns>
        internal static string ToPath(this Stack<string> stack) =>
            string.Join("/", stack.Reverse());

        /// <summary>
        /// Gets the path to the data file from stacked path elements.
        /// </summary>
        /// <param name="stack">The stack.</param>
        /// <returns>A <see cref="string"/> value containing the path to the data file.</returns>
        internal static string ToDataPath(this Stack<string> stack)
        {
            var path = stack.ToPath();
            if (!string.IsNullOrEmpty(path))
            {
                path += "/";
            }
            return path + FileSystemStorage.DataFile;
        }
    }
}
