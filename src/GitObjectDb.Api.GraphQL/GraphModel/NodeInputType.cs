using GitObjectDb.Api.GraphQL.Tools;
using GraphQL;
using GraphQL.Types;
using Namotion.Reflection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.GraphModel;

public class NodeInputType<TNode> : InputObjectGraphType<TNode>, INodeType<GitObjectDbMutation>
    where TNode : Node
{
    public NodeInputType()
    {
        Name = typeof(TNode).Name.Replace("`", string.Empty) + "Input";
        Description = typeof(TNode).GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false });

        AddReferences();
    }

    void INodeType<GitObjectDbMutation>.AddFieldsThroughReflection(GitObjectDbMutation mutation)
    {
        AddScalarProperties(mutation);
    }

    private void AddScalarProperties(GitObjectDbMutation mutation)
    {
        foreach (var property in typeof(TNode).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.PropertyType.IsNode() ||
                property.PropertyType.IsNodeEnumerable(out var _) ||
                !property.PropertyType.IsValidClrTypeForGraph(mutation.Schema))
            {
                continue;
            }
            var type = property.PropertyType.GetGraphTypeFromType(isNullable: true, TypeMappingMode.InputType);
            var summary = property.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false });
            Field(property.Name, type).Description(summary);
        }
    }

    private void AddReferences()
    {
        foreach (var property in typeof(TNode).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (Fields.Any(f => f.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (property.PropertyType.IsAssignableTo(typeof(Node)))
            {
                AddSingleReference(property);
            }
            if (property.PropertyType.IsNodeEnumerable(out var _))
            {
                AddMultiReference(property);
            }
        }
    }

    private void AddSingleReference(MemberInfo member) =>
        Field<GraphQLClrInputTypeReference<string>>(member.Name)
        .Description(
            typeof(TNode).GetProperty(member.Name)?.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }) ??
            member.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }));

    private void AddMultiReference(MemberInfo member) =>
        Field<ListGraphType<GraphQLClrInputTypeReference<string>>>(member.Name)
        .Description(
            typeof(TNode).GetProperty(member.Name)?.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }) ??
            member.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }));
}
