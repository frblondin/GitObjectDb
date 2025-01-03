using GitObjectDb.Api.GraphQL.Graph;
using GitObjectDb.Api.GraphQL.Graph.Scalars;
using GitObjectDb.Api.GraphQL.Model;
using GraphQL;
using GraphQL.Types;
using Namotion.Reflection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.Graph.Objects;

/// <summary>Represents an input type for a node in the GraphQL schema.</summary>
/// <typeparam name="TDto">The type of the data transfer object.</typeparam>
public class NodeInputType<TDto> : InputObjectGraphType<TDto>, INodeType<Mutation>
    where TDto : NodeInputDto
{
    /// <summary>Initializes a new instance of the <see cref="NodeInputType{TDto}"/> class.</summary>
    public NodeInputType()
    {
        Name = NodeType.Name.Replace("`", string.Empty) + "Input";
        Description = NodeType.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false });
    }

    /// <summary>Gets the type of the node.</summary>
    public static Type NodeType { get; } = typeof(TDto).BaseType!.GetGenericArguments()[0];

    /// <summary>Adds fields to the mutation through reflection.</summary>
    /// <param name="mutation">The mutation to add fields to.</param>
    void INodeType<Mutation>.AddFieldsThroughReflection(Mutation mutation)
    {
        AddScalarProperties(mutation);
    }

    private void AddScalarProperties(Mutation mutation)
    {
        foreach (var property in typeof(TDto).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.PropertyType.IsValidScalarForGraph(mutation.Schema))
            {
                continue;
            }
            var type = property.PropertyType.GetGraphTypeFromType(isNullable: true, TypeMappingMode.InputType);
            var matchingNodeProperty = NodeType.GetProperty(property.Name);
            var summary = matchingNodeProperty?.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false });
            Field(property.Name, type).Description(summary);
        }
    }
}
