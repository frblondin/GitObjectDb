using System;
using System.Collections;
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

    public object? CreatePrimitive(Type type) =>
        _inner.CreatePrimitive(type);

    public bool GetDictionary(IObjectDescriptor descriptor, out IDictionary? dictionary, out Type[]? genericArguments) =>
        _inner.GetDictionary(descriptor, out dictionary, out genericArguments);

    public Type GetValueType(Type type) =>
        _inner.GetValueType(type);

    public void ExecuteOnDeserializing(object value) =>
        _inner.ExecuteOnDeserializing(value);

    public void ExecuteOnDeserialized(object value) =>
        _inner.ExecuteOnDeserialized(value);

    public void ExecuteOnSerializing(object value) =>
        _inner.ExecuteOnSerializing(value);

    public void ExecuteOnSerialized(object value) =>
        _inner.ExecuteOnSerialized(value);
}
