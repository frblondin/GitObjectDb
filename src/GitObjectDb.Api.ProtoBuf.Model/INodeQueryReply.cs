using LibGit2Sharp;

namespace GitObjectDb.Api.ProtoBuf.Model;
internal interface INodeQueryReply
{
    /// <summary>Gets or sets all node serialized values, used by surrogate deserializers.</summary>
    IEnumerable<NodeData>? NodeContents { get; set; }

    /// <summary>Gets a cache to be used while processing deserialization of reply data.</summary>
    Dictionary<(DataPath Path, ObjectId TreeId), Node> Cache { get; }
}

internal static class NodeQueryReply
{
    private static readonly AsyncLocal<INodeQueryReply?> _current = new();

    public static INodeQueryReply Current
    {
        get => _current.Value ?? throw new NotSupportedException();
        set => _current.Value = value;
    }

    public static void ResetCurrent()
    {
        _current.Value = null;
    }
}