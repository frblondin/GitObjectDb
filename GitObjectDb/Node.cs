using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace GitObjectDb
{
    /// <summary>Represents a single object stored in the repository.</summary>
    [DebuggerDisplay("Path = {Path}")]
    public abstract class Node
    {
        /// <summary>Initializes a new instance of the <see cref="Node"/> class.</summary>
        /// <param name="id">The unique identifier.</param>
        public Node(UniqueId id)
        {
            Id = id;
        }

        /// <summary>Gets the unique node identifier.</summary>
        public UniqueId Id { get; }

        /// <summary>Gets the node path.</summary>
        [JsonIgnore]
        public Path Path { get; internal set; }
    }
}
