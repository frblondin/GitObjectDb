using GitObjectDb.Model;
using GitObjectDb.Tools;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace GitObjectDb.Serialization.Json;

internal class NodeTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public NodeTypeInfoResolver(IDataModel model)
    {
        Model = model;
    }

    public IDataModel Model { get; }

    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var result = base.GetTypeInfo(type, options);

        if (typeof(Node).IsAssignableFrom(type))
        {
            AddPolymorphismOptions(type, result);
        }

        return result;
    }

    private void AddPolymorphismOptions(Type type, JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
        };
        AddDerivedTypes(type, jsonTypeInfo.PolymorphismOptions.DerivedTypes);
    }

    private void AddDerivedTypes(Type nodeType, ICollection<JsonDerivedType> derivedTypes)
    {
        foreach (var description in Model.NodeTypes)
        {
            if (nodeType.IsAssignableFrom(description.Type))
            {
                derivedTypes.Add(new(description.Type, TypeHelper.BindToName(description.Type)));
            }
        }
    }
}
