using LibGit2Sharp;
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

    /// <summary>Initializes a new instance of the <see cref="Node"/> class from an existing instance.</summary>
    /// <param name="original">The original value to use.</param>
    public Node(Node original)
        : base(original)
    {
        // We want to copy all property values except TreeId
        (Id, EmbeddedResource, RemoteResource) = (original.Id, original.EmbeddedResource, original.RemoteResource);
    }

    /// <summary>Gets the unique node identifier.</summary>
    public UniqueId Id { get; init; } = UniqueId.CreateNew();

    /// <summary>Gets the id of the Git tree containing this node.</summary>
    [IgnoreDataMember]
    public ObjectId? TreeId { get; init; }

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
