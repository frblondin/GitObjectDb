using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Types;
using Namotion.Reflection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.GraphModel;

public class NodeInputType<TNode, TNodeDto> : InputObjectGraphType<TNodeDto>
    where TNode : Node
    where TNodeDto : NodeDto
{
    public NodeInputType()
    {
        Name = typeof(TNode).Name.Replace("`", string.Empty) + "Input";
        Description = typeof(TNode).GetXmlDocsSummary(false);

        AddScalarProperties();
        AddReferences();
    }

    private void AddScalarProperties()
    {
        foreach (var property in typeof(TNodeDto).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!AdditionalTypeMappings.IsScalarType(property.PropertyType))
            {
                continue;
            }
            var type = property.PropertyType.GetGraphTypeFromType(isNullable: true, TypeMappingMode.InputType);
            var summary = typeof(TNode).GetProperty(property.Name)?.GetXmlDocsSummary(false) ??
                property.GetXmlDocsSummary(false);
            Field(property.Name, type).Description(summary);
        }
    }

    private void AddReferences()
    {
        foreach (var property in typeof(TNodeDto).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (Fields.Any(f => f.Name == property.Name))
            {
                continue;
            }

            if (property.PropertyType.IsAssignableTo(typeof(NodeDto)))
            {
                AddSingleReference(property);
            }
            if (property.IsEnumerable(t => t.IsAssignableTo(typeof(NodeDto)), out var dtoType))
            {
                AddMultiReference(property);
            }
        }
    }

    private void AddSingleReference(MemberInfo member) =>
        Field<GraphQLClrInputTypeReference<string>>(member.Name)
        .Description(
            typeof(TNode).GetProperty(member.Name)?.GetXmlDocsSummary(false) ??
            member.GetXmlDocsSummary(false));

    private void AddMultiReference(MemberInfo member) =>
        Field<ListGraphType<GraphQLClrInputTypeReference<string>>>(member.Name)
        .Description(
            typeof(TNode).GetProperty(member.Name)?.GetXmlDocsSummary(false) ??
            member.GetXmlDocsSummary(false));
}
