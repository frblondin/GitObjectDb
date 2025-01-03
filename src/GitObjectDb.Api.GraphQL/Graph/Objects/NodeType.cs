using Fasterflect;
using GitObjectDb.Api.GraphQL.Graph;
using GitObjectDb.Api.GraphQL.Graph.Scalars;
using GitObjectDb.Api.GraphQL.Queries;
using GitObjectDb.Api.GraphQL.Tools;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Resolvers;
using GraphQL.Types;
using Namotion.Reflection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.Graph.Objects;

/// <summary>Represents a GraphQL node type.</summary>
/// <typeparam name="TNode">The type of the node.</typeparam>
public class NodeType<TNode> : ObjectGraphType<TNode>, INodeType<Query>
    where TNode : Node
{
    /// <summary>Initializes a new instance of the <see cref="NodeType{TNode}"/> class.</summary>
    public NodeType()
    {
        Name = typeof(TNode).Name.Replace("`", string.Empty);
        Description = typeof(TNode).GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false });

        Interface<NodeInterfaceType>();

        AddChildrenField();
        AddHistoryField();
    }

    private void AddChildrenField() =>
        NodeInterfaceType.CreateChildrenField(this)
        .ResolveThroughDI().UsingLoader<NodeDataLoaderKey, NodeLoader<Node>>();

    private void AddHistoryField() =>
        NodeInterfaceType.CreateHistoryField(this)
        .ResolveThroughDI().UsingLoader<NodeHistoryQueryKey, NodeHistoryLoader>();

    void INodeType<Query>.AddFieldsThroughReflection(Query query)
    {
        AddScalarProperties(query);
        AddReferences(query);
        AddChildren(query);
    }

    private void AddScalarProperties(Query query)
    {
        foreach (var property in typeof(TNode).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.PropertyType.IsNode() ||
                property.PropertyType.IsNodeEnumerable(out var _) ||
                !property.PropertyType.IsValidScalarForGraph(query.Schema))
            {
                continue;
            }
            var type = property.PropertyType.GetGraphTypeFromType(isNullable: true, TypeMappingMode.OutputType);
            var summary =
                typeof(TNode).GetProperty(property.Name)?.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }) ??
                property.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false });
            Field(property.Name, type)
                .Description(summary);
        }
    }

    private void AddReferences(Query query)
    {
        foreach (var property in typeof(TNode).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (Fields.Any(f => f.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (property.PropertyType.IsNode())
            {
                AddSingleReference(query, property);
            }
            if (property.PropertyType.IsNodeEnumerable(out var nodeType))
            {
                AddMultiReference(query, property, nodeType!);
            }
        }
    }

    private void AddSingleReference(Query query, PropertyInfo property)
    {
        var type = query.GetOrCreateGraphType(property.PropertyType);
        var getter = Reflect.PropertyGetter(property);

        Field(property.Name, type)
            .Description(property.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }) ??
                property.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }))
            .Resolve(new FuncFieldResolver<object?, object?>(context =>
            {
                var parentNode = context.Source as Node ??
                    throw new RequestError("Could not get parent node.");
                return getter.Invoke(parentNode);
            }));
    }

    private void AddMultiReference(Query query, PropertyInfo property, Type nodeType)
    {
        var type = query.GetOrCreateGraphType(nodeType);
        var getter = Reflect.PropertyGetter(property);

        Field(property.Name, type)
            .Description(property.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }) ??
                property.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }))
            .Resolve(new FuncFieldResolver<object?, object?>(context =>
            {
                var parentNode = context.Source as Node ??
                    throw new RequestError("Could not get parent node.");
                return getter.Invoke(parentNode);
            }));
    }

    private void AddChildren(Query query)
    {
        var description = query.Schema.Model.GetDescription(typeof(TNode));
        foreach (var childType in description.Children)
        {
            query.AddNodeListField(this, childType);
        }
    }
}
