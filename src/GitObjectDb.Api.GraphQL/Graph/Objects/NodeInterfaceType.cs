using GitObjectDb.Api.GraphQL.Graph.Scalars;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using LibGit2Sharp;
using Namotion.Reflection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.Graph.Objects;

internal sealed class NodeInterfaceType : InterfaceGraphType<Node>
{
    public NodeInterfaceType()
    {
        Name = nameof(Node);

        AddScalarProperties();
        CreateChildrenField(this);
        CreateHistoryField(this);
    }

    internal static NodeInterfaceType Instance { get; } = new NodeInterfaceType();

    private void AddScalarProperties()
    {
        foreach (var property in typeof(Node).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.PropertyType.IsValidScalarForGraph())
            {
                continue;
            }
            var type = property.PropertyType.GetGraphTypeFromType(isNullable: true, TypeMappingMode.OutputType);
            var summary = typeof(Node).GetProperty(property.Name)?.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false });
            Field(property.Name, type).Description(summary);
        }
    }

    internal static FieldBuilder<TSource, IEnumerable<Node>> CreateChildrenField<TSource>(ComplexGraphType<TSource> graph) =>
        graph.Field<ListGraphType<NodeInterfaceType>, IEnumerable<Node>>("Children")
        .Description("Gets the node children.");

    internal static FieldBuilder<TSource, IEnumerable<Commit>> CreateHistoryField<TSource>(ComplexGraphType<TSource> graph) =>
        graph.Field<ListGraphType<CommitType>, IEnumerable<Commit>>("History")
        .Description("Gets the history of node changes.");
}
