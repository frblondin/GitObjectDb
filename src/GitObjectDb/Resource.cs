using System;

namespace GitObjectDb;

/// <summary>Resource used by a <see cref="Node"/>.</summary>
public sealed partial record Resource : TreeItem
{
    /// <summary>Initializes a new instance of the <see cref="Resource"/> class.</summary>
    /// <param name="node">The node this resources will belong to.</param>
    /// <param name="folderPath">The path within the resource folder.</param>
    /// <param name="file">The file name.</param>
    /// <param name="embedded">The resource content.</param>
    public Resource(Node node, string folderPath, string file, Data embedded)
        : this(
              (node.Path ?? throw new ArgumentNullException(nameof(node), $"{nameof(Node.Path)} is null.")).CreateResourcePath(folderPath, file),
              embedded)
    {
        FileSystemStorage.ThrowIfAnyReservedName(folderPath);
    }

    internal Resource(DataPath path, Data embedded)
    {
        Path = path;
        Embedded = embedded;
    }

    /// <summary>Gets the embedded resource.</summary>
    public Data Embedded { get; }
}
