using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;

namespace GitObjectDb
{
    /// <summary>Represents a single object stored in the repository.</summary>
    [DebuggerDisplay("Id = {Id}, Path = {Path}")]
    public record Node : ITreeItem
    {
        /// <summary>Gets or sets the unique node identifier.</summary>
        public UniqueId Id { get; set; } = UniqueId.CreateNew();

        /// <summary>Gets or sets the node path.</summary>
        [JsonIgnore]
        public DataPath? Path { get; set; }

        /// <inheritdoc/>
        public override string ToString() => Id.ToString();
    }
}
