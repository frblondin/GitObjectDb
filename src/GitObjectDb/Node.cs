using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace GitObjectDb;

/// <summary>Represents a single object stored in the repository.</summary>
[DebuggerDisplay("Id = {Id}, Path = {Path}")]
public record Node : ITreeItem
{
    /// <summary>Initializes a new instance of the <see cref="Node"/> class.</summary>
    protected Node()
    {
    }

    /// <summary>Gets the unique node identifier.</summary>
    public UniqueId Id { get; init; } = UniqueId.CreateNew();

    /// <summary>Gets or sets the node path.</summary>
    [JsonIgnore]
    public DataPath? Path { get; set; }

    /// <summary>Gets the embedded resource.</summary>
    [JsonIgnore]
    public string? EmbeddedResource { get; init; }

    /// <inheritdoc/>
    public override string ToString() => Id.ToString();
}
