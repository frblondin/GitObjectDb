using Fasterflect;
using GitObjectDb.Api.ProtoBuf.Model;
using GitObjectDb.Comparison;
using GitObjectDb.Tools;
using LibGit2Sharp;
using ProtoBuf.Grpc;
using System.Reflection;

namespace GitObjectDb.Api.ProtoBuf;
internal class NodeQueryService<TNode> : INodeQueryService<TNode>
    where TNode : Node
{
    private readonly IConnection _connection;

    public NodeQueryService(IConnection connection)
    {
        _connection = connection;
    }

    public Task<NodeQueryReply<TNode>> QueryNodesAsync(NodeQueryRequest request, CallContext context = default)
    {
        var commit = _connection.Repository.Lookup<Commit>(request.Committish) ??
            throw new GitObjectDbInvalidCommitException();
        var treeId = commit.Tree.Id;
        return QueryNodesAsync(request, treeId);
    }

    internal Task<NodeQueryReply<TNode>> QueryNodesAsync(NodeQueryRequest request, ObjectId treeId)
    {
        request.Validate();
        var parent = request.ParentPath is not null ?
            _connection.Lookup<Node>(request.Committish!, request.ParentPath) :
            null;
        var result = _connection.GetNodes<TNode>(request.Committish!, parent, request.IsRecursive).ToList();
        var nodeContents = SerializeNodeDataRecursively(result, new());
        return Task.FromResult(
            new NodeQueryReply<TNode>(nodeContents, treeId, result));
    }

    public Task<NodeDeltaQueryReply<TNode>> QueryNodeDeltasAsync(NodeDeltaQueryRequest request, CallContext context = default)
    {
        request.Validate();

        var changes = _connection.Compare(request.Start!, request.End!);
        var result = from c in changes
                     where c.Old is TNode || c.New is TNode
                     select new NodeDelta<TNode>(c.Old as TNode, c.New as TNode, changes.End.Id, c.New is null);
        var nodeContents = SerializeNodeDataRecursively(changes.SelectMany(GetAllNodes).Distinct(), new());
        return Task.FromResult(
            new NodeDeltaQueryReply<TNode>(nodeContents, result));

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

    internal record struct NodeReference(DataPath Path, ObjectId TreeId)
    {
        public static implicit operator (DataPath Path, ObjectId TreeId)(NodeReference value) =>
            (value.Path, value.TreeId);

        public static implicit operator NodeReference((DataPath Path, ObjectId TreeId) value) =>
            new(value.Path, value.TreeId);
    }
}