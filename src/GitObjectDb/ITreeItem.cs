using System;
using System.Diagnostics.CodeAnalysis;

namespace GitObjectDb
{
    /// <summary>
    /// Represents a blob (<see cref="Node"/>, <see cref="Resource"/>...) managed by
    /// the engine.
    /// </summary>
    public interface ITreeItem
    {
        /// <summary>Gets or sets the blob path.</summary>
        DataPath? Path { get; set; }
    }

    internal static class ITreeItemExtensions
    {
        [ExcludeFromCodeCoverage]
        internal static DataPath ThrowIfNoPath(this ITreeItem item)
        {
            if (item.Path is null)
            {
                throw new InvalidOperationException("Item has no path defined.");
            }
            return item.Path;
        }
    }
}