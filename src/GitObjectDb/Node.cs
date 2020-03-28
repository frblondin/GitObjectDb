using System.Diagnostics;
using System.Text.Json.Serialization;

namespace GitObjectDb
{
    /// <summary>Represents a single object stored in the repository.</summary>
    [DebuggerDisplay("Path = {Path}")]
    public abstract class Node : ITreeItem, ITreeItemInternal
    {
        /// <summary>Initializes a new instance of the <see cref="Node"/> class.</summary>
        /// <param name="id">The unique identifier.</param>
        protected Node(UniqueId id)
        {
            Id = id;
        }

        /// <summary>Gets the unique node identifier.</summary>
        public UniqueId Id { get; }

        /// <summary>Gets the node path.</summary>
        [JsonIgnore]
        public DataPath? Path { get; internal set; }

        DataPath? ITreeItemInternal.Path
        {
            get => Path;
            set => Path = value;
        }
    }
}
