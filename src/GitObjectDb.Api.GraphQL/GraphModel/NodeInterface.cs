using GitObjectDb.Api.GraphQL.Tools;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using Namotion.Reflection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.GraphModel;

internal sealed class NodeInterface : InterfaceGraphType<Node>
{
    public NodeInterface()
    {
        Name = nameof(Node);

        AddScalarProperties();
        CreateChildrenField(this);
        CreateHistoryField(this);
    }

    private void AddScalarProperties()
    {
        foreach (var property in typeof(Node).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.PropertyType.IsValidClrTypeForGraph())
            {
                continue;
            }
            var type = property.PropertyType.GetGraphTypeFromType(isNullable: true, TypeMappingMode.OutputType);
            var summary = typeof(Node).GetProperty(property.Name)?.GetXmlDocsSummary(false);
            Field(property.Name, type).Description(summary);
        }
    }

    internal static FieldBuilder<TSource, object> CreateChildrenField<TSource>(ComplexGraphType<TSource> graph) =>
        graph.Field<ListGraphType<NodeInterface>>("Children")
        .Description("Gets the node children.");

    internal static FieldBuilder<TSource, object> CreateHistoryField<TSource>(ComplexGraphType<TSource> graph) =>
        graph.Field<ListGraphType<CommitType>>("History")
        .Description("Gets the history of node changes.");
}
