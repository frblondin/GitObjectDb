using System;

namespace GitObjectDb
{
    /// <summary>Resource used by a <see cref="Node"/>.</summary>
    public sealed partial class Resource : ITreeItem
    {
        private readonly Lazy<DataPath> _nodePath;

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
            _nodePath = new Lazy<DataPath>(Path.GetParentNode);
            Embedded = embedded;
        }

        /// <summary>Gets or sets the resource path.</summary>
        public DataPath Path { get; set; }

        /// <summary>Gets the embedded resource.</summary>
        public Data Embedded { get; }

        DataPath? ITreeItem.Path
        {
            get => Path;
            set => Path = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Gets the path of the node this resource belongs to.</summary>
        public DataPath NodePath => _nodePath.Value;
    }
}
