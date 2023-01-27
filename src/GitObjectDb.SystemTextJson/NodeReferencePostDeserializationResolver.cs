using Fasterflect;
using GitObjectDb.SystemTextJson.Tools;
using GitObjectDb.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace GitObjectDb.SystemTextJson;

/// <summary>
/// Due to circular references and node depreciation where types get transformed from (serialized deserialized)
/// node types into new types, this class intends to resolve references as a post treatment.
/// Two methods are involved:
/// <list type="bullet">
/// <item>
/// <term><see cref="ReadReferencePaths(Node, JsonDocument, DataPath)"/></term>
/// <description>Reads Json reference data (paths) and stores it into
/// <see cref="NodeReferenceHandler.DataContext"/>.</description>
/// </item>
/// <item>
/// <term><see cref="ResolveReferencesFromPaths(NodeReferenceHandler.DataContext, INodeSerializer.ItemLoader)"/></term>
/// <description>Resolves (deserialize) references recursively.</description>
/// </item>
/// </list>
/// </summary>
internal class NodeReferencePostDeserializationResolver
{
    private const string RefPropertyName = "$ref";
    private const string ValuesPropertyName = "$values";

    private readonly NodeSerializer _serializer;
    private readonly Dictionary<DataPath, List<ReferenceData>> _referencesToBeResolved = new();

    public NodeReferencePostDeserializationResolver(NodeSerializer serializer)
    {
        _serializer = serializer;
    }

    internal void ReadReferencePaths(Node result,
                                     JsonDocument document,
                                     DataPath path)
    {
        var description = _serializer.Model.GetDescription(result.GetType());
        foreach (var property in description.SerializableProperties)
        {
            if (property.PropertyType.IsNode())
            {
                ReadSingleReferencePath(document, path, property);
            }
            else if (property.PropertyType.IsNodeEnumerable(out var _))
            {
                ReadMultiReferencePaths(document, path, property);
            }
        }
    }

    private void ReadSingleReferencePath(JsonDocument document,
                                         DataPath path,
                                         PropertyInfo property)
    {
        var name = _serializer.Options.PropertyNamingPolicy?.ConvertName(property.Name) ??
            property.Name;
        if (document.RootElement.TryGetProperty(name, out var element))
        {
            var referenceId = element.GetProperty(RefPropertyName).GetString()!;
            var @ref = DataPath.Parse(referenceId);
            var data = GetNodeReferenceData(path);
            data.Add((property, new() { @ref! }));
        }
    }

    private void ReadMultiReferencePaths(JsonDocument document,
                                         DataPath path,
                                         PropertyInfo property)
    {
        var name = _serializer.Options.PropertyNamingPolicy?.ConvertName(property.Name) ??
            property.Name;
        if (document.RootElement.TryGetProperty(name, out var element) &&
            element.TryGetProperty(ValuesPropertyName, out var array) &&
            array.ValueKind == JsonValueKind.Array)
        {
            var values = from objectElement in array.EnumerateArray()
                         let referenceId = objectElement.GetProperty(RefPropertyName).GetString()!
                         select DataPath.Parse(referenceId);
            var data = GetNodeReferenceData(path);
            data.Add((property, values.ToList()));
        }
    }

    private IList<ReferenceData> GetNodeReferenceData(DataPath path)
    {
        if (!_referencesToBeResolved.TryGetValue(path, out var data))
        {
            _referencesToBeResolved[path] = data = new();
        }
        return data;
    }

    internal void ResolveReferencesFromPaths(NodeReferenceHandler.DataContext context,
                                             INodeSerializer.ItemLoader referenceResolver)
    {
        while (_referencesToBeResolved.Count > 0)
        {
            var kvp = _referencesToBeResolved.First();
            var node = context.Resolver.Items[kvp.Key];
            foreach (var (property, values) in kvp.Value)
            {
                if (property.PropertyType.IsNode())
                {
                    ResolveSingleReferenceFromPath(node, property, values.Single(), context, referenceResolver);
                }
                else if (property.PropertyType.IsNodeEnumerable(out var elementType))
                {
                    ResolveMultiReferencesFromPaths(node, property, values, elementType!, context, referenceResolver);
                }
            }
            _referencesToBeResolved.Remove(kvp.Key);
        }
    }

    private static void ResolveSingleReferenceFromPath(TreeItem node,
                                                       PropertyInfo property,
                                                       DataPath path,
                                                       NodeReferenceHandler.DataContext context,
                                                       INodeSerializer.ItemLoader referenceResolver)
    {
        var reference = ResolveReferenceFromContext(path, context, referenceResolver);
        var setter = Reflect.PropertySetter(property);
        setter(node, reference);
    }

    private static void ResolveMultiReferencesFromPaths(TreeItem node,
                                                        PropertyInfo property,
                                                        List<DataPath> paths,
                                                        Type elementType,
                                                        NodeReferenceHandler.DataContext context,
                                                        INodeSerializer.ItemLoader referenceResolver)
    {
        var references = paths.Select(p => ResolveReferenceFromContext(p, context, referenceResolver));
        var value = EnumerableFactory.Get(elementType).Create(property.PropertyType, references);
        var setter = Reflect.PropertySetter(property);
        setter(node, value);
    }

    private static Node ResolveReferenceFromContext(DataPath path,
                                                    NodeReferenceHandler.DataContext context,
                                                    INodeSerializer.ItemLoader referenceResolver)
    {
        if (!context.Resolver.Items.TryGetValue(path, out var reference))
        {
            context.Resolver.Items[path] = reference = referenceResolver(path);
        }

        return (Node)reference;
    }

    internal record struct ReferenceData(PropertyInfo Property, List<DataPath> References)
    {
        public static implicit operator (PropertyInfo Property, List<DataPath> References)(ReferenceData value) =>
            (value.Property, value.References);

        public static implicit operator ReferenceData((PropertyInfo Property, List<DataPath> References) value) =>
            new(value.Property, value.References);
    }
}
