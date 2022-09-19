using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using Namotion.Reflection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.GraphModel;

internal sealed class NodeInterface : InterfaceGraphType<NodeDto>
{
    public NodeInterface()
    {
        Name = nameof(Node);

        Field(n => n.Children).Description("Gets the node children.");

        foreach (var property in typeof(NodeDto).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!SchemaTypes.BuiltInScalarMappings.ContainsKey(property.PropertyType))
            {
                continue;
            }
            var type = property.PropertyType.GetGraphTypeFromType(isNullable: true, TypeMappingMode.OutputType);
            var summary = typeof(Node).GetProperty(property.Name)?.GetXmlDocsSummary(false);
            Field(property.Name, type).Description(summary);
        }

        CreateHistoryField(this);
    }

    internal static FieldBuilder<TSource, object> CreateHistoryField<TSource>(ComplexGraphType<TSource> graph) =>
        graph.Field<ListGraphType<CommitType>>("History")
        .Description("Gets the history of node changes.");
}
