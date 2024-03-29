using System;
using System.IO;
using YamlDotNet.Serialization;

namespace GitObjectDb.YamlDotNet.Core;

internal class NodeFactory : IObjectFactory
{
    private readonly IObjectFactory _inner;

    public NodeFactory(IObjectFactory inner)
    {
        _inner = inner;
    }

    public object Create(Type type) =>
        typeof(Node).IsAssignableFrom(type) ?
        CreateNode(type) :
        _inner.Create(type);

    private static Node CreateNode(Type type)
    {
        var result = (Node)Activator.CreateInstance(type)!;

        // Make sure that nested lookups will find node to avoid stack overflows
        NodeReferenceParser.CurrentInstance.Nodes.Add(result);

        return result;
    }
}
