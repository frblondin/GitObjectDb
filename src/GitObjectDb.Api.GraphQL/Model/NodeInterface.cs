using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Types;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.Model;

internal sealed class NodeInterface : InterfaceGraphType<NodeDto>
{
    public NodeInterface()
    {
        Name = nameof(Node);

        foreach (var property in typeof(NodeDto).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!SchemaTypes.BuiltInScalarMappings.ContainsKey(property.PropertyType))
            {
                continue;
            }
            var type = property.PropertyType.GetGraphTypeFromType(isNullable: true, TypeMappingMode.OutputType);
            Field(type, property.Name);
        }

        AddField(new FieldType
        {
            Name = "History",
            Type = typeof(ListGraphType<CommitType>),
        });
    }
}
