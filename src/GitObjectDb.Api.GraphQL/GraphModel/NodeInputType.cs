using GitObjectDb.Api.GraphQL.Model;
using GraphQL;
using GraphQL.Types;
using Namotion.Reflection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.GraphModel;

public class NodeInputType<TDto> : InputObjectGraphType<TDto>, INodeType<GitObjectDbMutation>
    where TDto : NodeInputDto
{
    public NodeInputType()
    {
        Name = NodeType.Name.Replace("`", string.Empty) + "Input";
        Description = NodeType.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false });
    }

    public static Type NodeType { get; } = typeof(TDto).BaseType!.GetGenericArguments()[0];

    void INodeType<GitObjectDbMutation>.AddFieldsThroughReflection(GitObjectDbMutation mutation)
    {
        AddScalarProperties(mutation);
    }

    private void AddScalarProperties(GitObjectDbMutation mutation)
    {
        foreach (var property in typeof(TDto).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.PropertyType.IsValidClrTypeForGraph(mutation.Schema))
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
