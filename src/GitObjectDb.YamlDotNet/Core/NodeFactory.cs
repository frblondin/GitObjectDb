using System;
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

    private static object CreateNode(Type type)
    {
        var parser = NodeReferenceParser.CurrentInstance;

        var result = (Node)Activator.CreateInstance(type);
        result.Path = parser.Path;

        // Make sure that nested lookups will find node to avoid stack overflows
        parser.Nodes.Add(result);

        return result;
    }
}
