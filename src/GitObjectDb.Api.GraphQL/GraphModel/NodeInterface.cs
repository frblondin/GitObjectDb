using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Types;
using Namotion.Reflection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.GraphModel;

internal sealed class NodeInterface : InterfaceGraphType<NodeDto>
{
    public NodeInterface()
    {
        Name = nameof(Node);

        Field(n => n.Children);

        foreach (var property in typeof(NodeDto).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!SchemaTypes.BuiltInScalarMappings.ContainsKey(property.PropertyType))
            {
                continue;
            }
            var type = property.PropertyType.GetGraphTypeFromType(isNullable: true, TypeMappingMode.OutputType);
            var summary = typeof(Node).GetProperty(property.Name)?.GetXmlDocsSummary(false);
            Field(type, property.Name, summary);
        }

        AddField(CreateHistoryField());
    }

    internal static FieldType CreateHistoryField() => new()
    {
        Name = "History",
        Type = typeof(ListGraphType<CommitType>),
        Description = "Gets the history of node changes.",
    };
}
