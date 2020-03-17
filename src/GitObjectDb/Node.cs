using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace GitObjectDb
{
    /// <summary>Represents a single object stored in the repository.</summary>
    [DebuggerDisplay("Path = {Path}, Resources = {Resources.Count}")]
    public abstract class Node : ITreeItem, ITreeItemInternal
    {
        /// <summary>Initializes a new instance of the <see cref="Node"/> class.</summary>
        /// <param name="id">The unique identifier.</param>
        public Node(UniqueId id)
        {
            Id = id;
            Resources = new ResourceCollection(this);
        }

        /// <summary>Gets the unique node identifier.</summary>
        public UniqueId Id { get; }

        /// <summary>
        /// Gets the resources declared by the object.
        /// </summary>
        [JsonIgnore]
        public ResourceCollection Resources { get; private set; }

        /// <summary>Gets the node path.</summary>
        [JsonIgnore]
        public DataPath Path { get; internal set; }

        DataPath ITreeItemInternal.Path
        {
            get => Path;
            set => Path = value;
        }
    }
}
