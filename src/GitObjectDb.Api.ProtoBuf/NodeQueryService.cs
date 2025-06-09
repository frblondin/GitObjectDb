using Fasterflect;
using GitDotNet;
using GitObjectDb.Api.ProtoBuf.Model;
using GitObjectDb.Comparison;
using GitObjectDb.Tools;
using ProtoBuf.Grpc;
using System.Reflection;
using Change = GitObjectDb.Comparison.Change;

namespace GitObjectDb.Api.ProtoBuf;
internal class NodeQueryService<TNode> : INodeQueryService<TNode>
    where TNode : Node
{
    private readonly IConnection _connection;

    public NodeQueryService(IConnection connection)
    {
        _connection = connection;
    }

    public async Task<NodeQueryReply<TNode>> QueryNodesAsync(NodeQueryRequest request, CallContext context = default)
    {
        var commit = (request.Committish != null ? await _connection.Repository.GetCommittishAsync(request.Committish) : null) ??
            throw new GitObjectDbInvalidCommitException();
        var tree = await commit.GetRootTreeAsync();
        return await QueryNodesAsync(request, tree.Id);
    }

    internal async Task<NodeQueryReply<TNode>> QueryNodesAsync(NodeQueryRequest request, HashId treeId)
    {
        request.Validate();
        var commit = await _connection.Repository.GetCommittishAsync(request.Committish!);
        var parent = request.ParentPath is not null ?
            await _connection.LookupAsync<Node>(commit, request.ParentPath) :
            null;
        var result = _connection.GetNodesAsync<TNode>(commit, parent, request.IsRecursive).ToEnumerable().ToList();
        var nodeContents = SerializeNodeDataRecursively(result, []);
        return new NodeQueryReply<TNode>(nodeContents, treeId, result);
    }

    public async Task<NodeDeltaQueryReply<TNode>> QueryNodeDeltasAsync(NodeDeltaQueryRequest request, CallContext context = default)
    {
        request.Validate();

        var changes = await _connection.CompareAsync(request.Start!, request.End!);
        var result = from c in changes
                     where c.Old is TNode || c.New is TNode
                     select new NodeDelta<TNode>(c.Old as TNode, c.New as TNode, changes.End.Id, c.New is null);
        var nodeContents = SerializeNodeDataRecursively(changes.SelectMany(GetAllNodes).Distinct(), new());
        return new NodeDeltaQueryReply<TNode>(nodeContents, result);

        static IEnumerable<Node> GetAllNodes(Change change)
        {
            if (change.Old is Node old)
            {
                yield return old;
            }
            if (change.New is Node @new)
            {
                yield return @new;
            }
        }
    }

    private IEnumerable<NodeData> SerializeNodeDataRecursively(IEnumerable<Node> nodes, Dictionary<NodeReference, byte[]> result)
    {
        foreach (var node in nodes)
        {
            var path = node.Path ?? throw new NotSupportedException("Missing path for node.");
            var treeId = node.TreeId ?? throw new NotSupportedException("Missing treeId for node.");
            if (node.Path is not null && !result.ContainsKey((path, treeId)))
            {
                result[(path, treeId)] = SerializeNode(node);
                SerializeNestedReferences(node, result);
            }
        }
        return from kvp in result
               select new NodeData(kvp.Key.Path,
                                   kvp.Key.TreeId,
                                   kvp.Value);
    }

    private byte[] SerializeNode(Node node)
    {
        using var stream = new MemoryStream();
        _connection.Serializer.Serialize(node, stream);
        return stream.ToArray();
    }

    private void SerializeNestedReferences(Node node, Dictionary<NodeReference, byte[]> result)
    {
        var description = _connection.Model.GetDescription(node.GetType());
        foreach (var property in description.SerializableProperties)
        {
            if (property.PropertyType.IsNode())
            {
                SerializeNestedNodeReference(node, property, result);
            }
            if (property.PropertyType.IsNodeEnumerable(out var _))
            {
                SerializeNestedNodeReferences(node, property, result);
            }
        }
    }

    private void SerializeNestedNodeReference(Node node, PropertyInfo property, Dictionary<NodeReference, byte[]> result)
    {
        var reference = (Node?)Reflect.PropertyGetter(property).Invoke(node);
        if (reference is not null)
        {
            SerializeNodeDataRecursively(Enumerable.Repeat(reference, 1), result);
        }
    }

    private void SerializeNestedNodeReferences(Node node, PropertyInfo property, Dictionary<NodeReference, byte[]> result)
    {
        var references = (IEnumerable<Node>?)Reflect.PropertyGetter(property).Invoke(node);
        if (references is not null)
        {
            SerializeNodeDataRecursively(references, result);
        }
    }

    internal record struct NodeReference(DataPath Path, HashId TreeId)
    {
        public static implicit operator (DataPath Path, HashId TreeId)(NodeReference value) =>
            (value.Path, value.TreeId);

        public static implicit operator NodeReference((DataPath Path, HashId TreeId) value) =>
            new(value.Path, value.TreeId);
    }
}