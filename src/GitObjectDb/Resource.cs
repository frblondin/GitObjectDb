using LibGit2Sharp;
using System;
using System.IO;

namespace GitObjectDb
{
    /// <summary>Resource used by a <see cref="Node"/>.</summary>
    public sealed partial class Resource : ITreeItem
    {
        private readonly Lazy<DataPath> _nodePath;
        private Func<Stream> _value;

        /// <summary>Initializes a new instance of the <see cref="Resource"/> class.</summary>
        /// <param name="node">The node this resources will belong to.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <param name="value">The resource content.</param>
        public Resource(Node node, DataPath relativePath, string value)
            : this(
                  DataPath.FromGitBlobPath(
                      $"{(node.Path ?? throw new ArgumentNullException(nameof(node), $"{nameof(Node.Path)} is null.")).FolderPath}/" +
                      $"{FileSystemStorage.ResourceFolder}/{relativePath.FilePath}"),
                  new StringReaderStream(value))
        {
            FileSystemStorage.ThrowIfAnyReservedName(relativePath.FilePath);
        }

        /// <summary>Initializes a new instance of the <see cref="Resource"/> class.</summary>
        /// <param name="parentPath">The parent path of the node this resources will belong to.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <param name="value">The resource content.</param>
        public Resource(DataPath parentPath, DataPath relativePath, string value)
            : this(
                  DataPath.FromGitBlobPath(
                      $"{parentPath.FolderPath}/" +
                      $"{FileSystemStorage.ResourceFolder}/{relativePath.FilePath}"),
                  new StringReaderStream(value))
        {
            FileSystemStorage.ThrowIfAnyReservedName(relativePath.FilePath);
        }

        /// <summary>Initializes a new instance of the <see cref="Resource"/> class.</summary>
        /// <param name="node">The node this resources will belong to.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <param name="stream">The resource content.</param>
        public Resource(Node node, DataPath relativePath, Stream stream)
            : this(
                  (node.Path ?? throw new ArgumentNullException(nameof(node), $"{nameof(Node.Path)} is null.")).CreateResourcePath(relativePath),
                  () => stream)
        {
            FileSystemStorage.ThrowIfAnyReservedName(relativePath.FilePath);
        }

        internal Resource(DataPath path, Blob blob)
            : this(path, blob.GetContentStream)
        {
        }

        internal Resource(DataPath path, Stream stream)
            : this(path, () => stream)
        {
        }

        private Resource(DataPath path, Func<Stream> value)
        {
            Path = path;
            _nodePath = new Lazy<DataPath>(Path.GetResourceParentNode);
            _value = value;
        }

        /// <summary>Gets or sets the resource path.</summary>
        public DataPath Path { get; set; }

        DataPath? ITreeItem.Path
        {
            get => Path;
            set => Path = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Gets the path of the node this resource belongs to.</summary>
        public DataPath NodePath => _nodePath.Value;

        /// <summary>Gets the content stream.</summary>
        /// <returns>The stream.</returns>
        public Stream GetContentStream() =>
            _value.Invoke();
    }
}
