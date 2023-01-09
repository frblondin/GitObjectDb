using LibGit2Sharp;
using ProtoBuf;
using System.Runtime.Serialization;

namespace GitObjectDb.Api.ProtoBuf.Model;

/// <summary>Represents a collection of changes that occurred in a repository.</summary>
/// <typeparam name="TNode">The type of the <see cref="Node"/>.</typeparam>
[ProtoContract]
public class NodeDeltaQueryReply<TNode> : INodeQueryReply
    where TNode : Node
{
    private NodeDeltaQueryReply()
    {
    }

    internal NodeDeltaQueryReply(IEnumerable<NodeData>? nodeContents, IEnumerable<NodeDelta<TNode>>? changes)
    {
        ((INodeQueryReply)this).NodeContents = nodeContents;
        Changes = changes;
    }

    /// <inheritdoc/>>
    [ProtoMember(1)]
    IEnumerable<NodeData>? INodeQueryReply.NodeContents { get; set; }

    /// <summary>Gets or sets all change descriptions.</summary>
    [ProtoMember(2)]
    public IEnumerable<NodeDelta<TNode>>? Changes { get; set; }

    Dictionary<(DataPath Path, ObjectId TreeId), Node> INodeQueryReply.Cache { get; } = new();

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0051 // Remove unused private members
    [ProtoBeforeDeserialization]
    private void BeforeDeserialize()
    {
        NodeQueryReply.Current = this;
    }

    [ProtoAfterDeserialization]
    private void AfterDeserialization()
    {
        NodeQueryReply.ResetCurrent();
    }
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CA1822 // Mark members as static
}

/// <summary>Represents changes that occurred to a <see cref="Node"/>.</summary>
/// <param name="Old">Gets the old state of the node.</param>
/// <param name="New">Gets the new state of the node.</param>
/// <param name="UpdatedAt">Gets the latest commit id.</param>
/// <param name="Deleted">Gets a value indicating whether the node has been deleted.</param>
/// <typeparam name="TNode">The type of the <see cref="Node"/>.</typeparam>
[ProtoContract]
public record NodeDelta<TNode>([property: ProtoMember(1)] TNode? Old,
                               [property: ProtoMember(2)] TNode? New,
                               [property: ProtoMember(3)] ObjectId? UpdatedAt,
                               [property: ProtoMember(4)] bool Deleted)
    where TNode : Node
{
    private NodeDelta()
        : this(null, null, null, false)
    {
    }
}
