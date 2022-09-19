using System.Diagnostics;
using System.Text.Json.Serialization;

namespace GitObjectDb
{
    /// <summary>Represents a single object stored in the repository.</summary>
    [DebuggerDisplay("Id = {Id}, Path = {Path}")]
    public abstract record Node : ITreeItem
    {
        /// <summary>Gets the unique node identifier.</summary>
        public UniqueId Id { get; init; } = UniqueId.CreateNew();

        /// <summary>Gets or sets the node path.</summary>
        [JsonIgnore]
        public DataPath? Path { get; set; }

        /// <summary>Gets or sets the embedded resource.</summary>
        [JsonIgnore]
        public string? EmbeddedResource { get; set; }

        /// <inheritdoc/>
        public override string ToString() => Id.ToString();
    }
}
