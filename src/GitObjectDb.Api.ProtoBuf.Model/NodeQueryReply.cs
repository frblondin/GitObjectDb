using LibGit2Sharp;
using ProtoBuf;

namespace GitObjectDb.Api.ProtoBuf.Model;

#pragma warning disable SA1402 // File may only contain a single type
/// <summary>Represents a query result.</summary>
/// <typeparam name="TNode">The type of the <see cref="Node"/>.</typeparam>
[ProtoContract]
public class NodeQueryReply<TNode> : INodeQueryReply
    where TNode : Node
{
    private NodeQueryReply()
    {
    }

    internal NodeQueryReply(IEnumerable<NodeData>? nodeContents, ObjectId? treeId, IEnumerable<TNode>? nodes)
    {
        ((INodeQueryReply)this).NodeContents = nodeContents;
        TreeId = treeId;
        Nodes = nodes;
    }

    [ProtoMember(1)]
    IEnumerable<NodeData>? INodeQueryReply.NodeContents { get; set; }

    /// <summary>Gets or sets the <see cref="ObjectId"/> of the tree containing returned nodes.</summary>
    [ProtoMember(2)]
    public ObjectId? TreeId { get; set; }

    /// <summary>Gets or sets the nodes returned by the query result.</summary>
    [ProtoMember(3)]
    public IEnumerable<TNode>? Nodes { get; set; }

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

/// <summary>Contains compressed serialized data of <see cref="Node"/> items.</summary>
/// <param name="Path">Gets or sets the path of the node.</param>
/// <param name="TreeId">Gets or sets the <see cref="ObjectId"/> of the tree containing the node.</param>
/// <param name="Data">Gets or sets the compressed serialized data of the node.</param>
[ProtoContract]
public record NodeData([property: ProtoMember(1)] DataPath? Path,
                       [property: ProtoMember(2)] ObjectId? TreeId,
                       [property: ProtoMember(3)] byte[]? Data)
{
    private NodeData()
        : this(null, null, null)
    {
    }

    internal DataPath GetPathOrThrow() =>
        Path ??
        throw new NotSupportedException($"No path provided in {nameof(Path)}.");
}
