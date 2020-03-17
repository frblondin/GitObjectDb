using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitObjectDb
{
    /// <summary>
    /// Resource used by a <see cref="Node"/>.
    /// </summary>
    public class Resource : ITreeItem
    {
        private Lazy<DataPath> _nodePath;
        private Func<Stream> _value;

        internal Resource(DataPath path, Blob blob)
            : this(path, blob.GetContentStream)
        {
            IsDetached = false;
        }

        internal Resource(DataPath path, byte[] values)
            : this(path, () => new MemoryStream(values))
        {
            IsDetached = true;
        }

        private Resource(DataPath path, Func<Stream> value)
        {
            Path = path;
            _nodePath = new Lazy<DataPath>(Path.GetResourceParentNode);
            _value = value;
        }

        /// <summary>Gets the resource path.</summary>
        public DataPath Path { get; }

        /// <summary>Gets the path of the node this resource belongs to.</summary>
        public DataPath NodePath => _nodePath.Value;

        internal bool IsDetached { get; }

        /// <summary>
        /// Gets the content stream.
        /// </summary>
        /// <returns>The stream.</returns>
        public Stream GetContentStream() =>
            _value.Invoke();

        /// <summary>
        /// Sets the content stream of this resource.
        /// </summary>
        /// <param name="stream">The content stream.</param>
        public void SetContentStream(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _value = () => stream;
        }
    }
}