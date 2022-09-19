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
        /// <param name="folderPath">The path within the resource folder.</param>
        /// <param name="file">The file name.</param>
        /// <param name="value">The resource content.</param>
        public Resource(Node node, string folderPath, string file, string value)
            : this(
                  (node.Path ?? throw new ArgumentNullException(nameof(node), $"{nameof(Node.Path)} is null.")).CreateResourcePath(folderPath, file),
                  new StringReaderStream(value))
        {
            FileSystemStorage.ThrowIfAnyReservedName(folderPath);
        }

        /// <summary>Initializes a new instance of the <see cref="Resource"/> class.</summary>
        /// <param name="parentPath">The parent path of the node this resources will belong to.</param>
        /// <param name="folderPath">The path within the resource folder.</param>
        /// <param name="file">The file name.</param>
        /// <param name="value">The resource content.</param>
        public Resource(DataPath parentPath, string folderPath, string file, string value)
            : this(
                  parentPath.CreateResourcePath(folderPath, file),
                  new StringReaderStream(value))
        {
            FileSystemStorage.ThrowIfAnyReservedName(folderPath);
        }

        /// <summary>Initializes a new instance of the <see cref="Resource"/> class.</summary>
        /// <param name="node">The node this resources will belong to.</param>
        /// <param name="folderPath">The path within the resource folder.</param>
        /// <param name="file">The file name.</param>
        /// <param name="stream">The resource content.</param>
        public Resource(Node node, string folderPath, string file, Stream stream)
            : this(
                  (node.Path ?? throw new ArgumentNullException(nameof(node), $"{nameof(Node.Path)} is null.")).CreateResourcePath(folderPath, file),
                  () => stream)
        {
            FileSystemStorage.ThrowIfAnyReservedName(folderPath);
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
