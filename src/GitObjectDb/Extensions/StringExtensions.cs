using GitObjectDb;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
        internal static string GetParentPath(this string path, int count = 1)
        {
            if (count == 1 && string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            var parts = GetParentChunks(path, count);

            return string.Join("/", parts.Take(parts.Length - count));
        }

        private static string[] GetParentChunks(string path, int count = 1)
        {
            var parts = path.Split('/');

            if (parts.Length < count)
            {
                throw new ArgumentException($"The parent path could not be found for '{path}'.", nameof(path));
            }

            return parts;
        }

        /// <summary>
        /// Returns the parent path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <exception cref="ArgumentException">path</exception>
        /// <returns>The parent path.</returns>
        internal static string GetDataParentDataPath(this string path)
        {
            return $"{path.GetParentPath(3)}/{FileSystemStorage.DataFile}";
        }

        /// <summary>
        /// Returns the parent path <see cref="UniqueId"/>.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="repository">The repository.</param>
        /// <exception cref="ArgumentException">path</exception>
        /// <returns>The parent <see cref="UniqueId"/>.</returns>
        internal static UniqueId GetDataParentId(this string path, IObjectRepository repository)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            var chunks = GetParentChunks(path, 0);
            return chunks.Length > 3 ?
                new UniqueId(chunks[chunks.Length - 4]) :
                repository.Id;
        }
    }
}
