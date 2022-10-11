using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace GitObjectDb;

/// <summary>
/// Represents a blob (<see cref="Node"/>, <see cref="Resource"/>...) managed by
/// the engine.
/// </summary>
public abstract record TreeItem
{
    /// <summary>Gets or sets the blob path.</summary>
    [IgnoreDataMember]
    public DataPath? Path { get; set; }
}

internal static class TreeItemExtensions
{
    [ExcludeFromCodeCoverage]
    internal static DataPath ThrowIfNoPath(this TreeItem item)
    {
        if (item.Path is null)
        {
            throw new InvalidOperationException("Item has no path defined.");
        }
        return item.Path;
    }
}
