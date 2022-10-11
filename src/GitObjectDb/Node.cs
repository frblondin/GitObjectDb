using System.Diagnostics;
using System.Runtime.Serialization;

namespace GitObjectDb;

/// <summary>Represents a single object stored in the repository.</summary>
[DebuggerDisplay("Id = {Id}, Path = {Path}")]
public record Node : TreeItem
{
    /// <summary>Initializes a new instance of the <see cref="Node"/> class.</summary>
    protected Node()
    {
    }

    /// <summary>Gets the unique node identifier.</summary>
    public UniqueId Id { get; init; } = UniqueId.CreateNew();

    /// <summary>Gets the embedded resource.</summary>
    [IgnoreDataMember]
    public string? EmbeddedResource { get; init; }

    /// <summary>
    /// Gets the remote repository containing node resources, as a
    /// replacement of resources stored in current repository.
    /// </summary>
    public ResourceLink? RemoteResource { get; init; }

    /// <inheritdoc/>
    public override string ToString() => Id.ToString();
}
