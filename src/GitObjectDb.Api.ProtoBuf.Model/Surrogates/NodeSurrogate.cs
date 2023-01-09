using GitObjectDb.Api.ProtoBuf.Model;
using LibGit2Sharp;
using ProtoBuf;

namespace GitObjectDb.Api.ProtoBuf.Model.Surrogates;
[ProtoContract]
internal class NodeSurrogate<TNode>
    where TNode : Node
{
    [ProtoMember(1)]
    public string? Path { get; init; }

    [ProtoMember(2)]
    public ObjectId? TreeId { get; init; }

    public static implicit operator NodeSurrogate<TNode>?(TNode? value)
    {
        if (value == null)
        {
            return null;
        }
        return new NodeSurrogate<TNode>
        {
            Path = value.Path!.FilePath,
            TreeId = value.TreeId,
        };
    }

    public static implicit operator TNode?(NodeSurrogate<TNode>? value)
    {
        if (value?.Path is null)
        {
            return null;
        }
        var currentReply = NodeQueryReply.Current;
        if (currentReply.NodeContents is null)
        {
            throw new NotImplementedException($"{nameof(INodeQueryReply)}.{nameof(INodeQueryReply.NodeContents)} should not be null.");
        }
        if (value.TreeId is null)
        {
            throw new NotImplementedException($"{nameof(NodeSurrogate<TNode>)}.{nameof(NodeSurrogate<TNode>.TreeId)} should not be null.");
        }
        var treeId = value.TreeId;
        var serializer = IServiceProviderExtensions.Serializer ??
            throw new NotImplementedException($"No serializer has been registered by calling " +
                                            $"{nameof(IServiceProviderExtensions)}." +
                                            $"{nameof(IServiceProviderExtensions.ConfigureGitObjectDbProtoRuntimeTypeModel)}.");
        return (TNode)GetNode(DataPath.Parse(value.Path));

        Node GetNode(DataPath path)
        {
            if (!currentReply.Cache.TryGetValue((path, treeId), out var result))
            {
                var content = currentReply.NodeContents.First(data => data.GetPathOrThrow().Equals(path) && data.TreeId == treeId);
                using var stream = GetStream(content);
                result = serializer.Deserialize(stream, treeId, path, GetNode);
                currentReply.Cache[(path, treeId)] = result;
            }
            return result;
        }
    }

    private static Stream GetStream(NodeData content)
    {
        var data = content.Data ?? throw new NotImplementedException($"No data provided in {nameof(NodeData)}.");
        return new MemoryStream(data)
        {
            Position = 0L,
        };
    }
}