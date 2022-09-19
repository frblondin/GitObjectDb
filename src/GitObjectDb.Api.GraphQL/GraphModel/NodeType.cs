using GitObjectDb.Api.GraphQL.Queries;
using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Types;
using Namotion.Reflection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.GraphModel;

public class NodeType<TNode, TNodeDto> : ObjectGraphType<TNodeDto>, INodeType
    where TNode : Node
    where TNodeDto : NodeDto
{
    public NodeType()
    {
        Name = typeof(TNode).Name.Replace("`", string.Empty);
        Description = typeof(TNode).GetXmlDocsSummary(false);

        Field(n => n.Children);

        AddScalarProperties();
        AddHistoryField();
        Interface<NodeInterface>();
    }

    private void AddHistoryField()
    {
        var historyField = AddField(NodeInterface.CreateHistoryField());
        historyField.Arguments = new(
                new QueryArgument<StringGraphType> { Name = GitObjectDbQuery.BranchArgument });
        historyField.Resolver = GraphQLHelper.CreateFieldResolver<object>(LogQuery.Execute);
    }

    private void AddScalarProperties()
    {
        foreach (var property in typeof(TNodeDto).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!AdditionalTypeMappings.IsScalarType(property.PropertyType))
            {
                continue;
            }
            var type = property.PropertyType.GetGraphTypeFromType(isNullable: true, TypeMappingMode.OutputType);
            var summary = typeof(TNode).GetProperty(property.Name)?.GetXmlDocsSummary(false) ??
                property.GetXmlDocsSummary(false);
            Field(property.Name, type)
                .Description(summary);
        }
    }

    void INodeType.AddReferences(GitObjectDbQuery query)
    {
        foreach (var property in typeof(TNodeDto).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (Fields.Any(f => f.Name == property.Name))
            {
                continue;
            }

            if (property.PropertyType.IsAssignableTo(typeof(NodeDto)))
            {
                AddSingleReference(query, property);
            }
            if (property.IsEnumerable(t => t.IsAssignableTo(typeof(NodeDto)), out var dtoType))
            {
                AddMultiReference(query, property, dtoType!);
            }
        }
    }

    private void AddSingleReference(GitObjectDbQuery query, PropertyInfo property)
    {
        var type = query.GetOrCreateGraphType(property.PropertyType, out var nodeType);

        AddField(new()
        {
            Name = property.Name,
            Description = typeof(TNode).GetProperty(property.Name)?.GetXmlDocsSummary(false) ??
                property.GetXmlDocsSummary(false),
            Type = type.GetType(),
            ResolvedType = type,
            Resolver = NodeReferenceQuery.CreateSingleReferenceResolver(nodeType, property),
        });
    }

    private void AddMultiReference(GitObjectDbQuery query, MemberInfo member, Type dtoType)
    {
        var type = query.GetOrCreateGraphType(dtoType, out var nodeType);

        AddField(new()
        {
            Name = member.Name,
            Description = typeof(TNode).GetProperty(member.Name)?.GetXmlDocsSummary(false) ??
                member.GetXmlDocsSummary(false),
            Type = type.GetType(),
            ResolvedType = type,
            Resolver = NodeReferenceQuery.CreateMultiReferenceResolver(dtoType, nodeType, member),
        });
    }

    void INodeType.AddChildren(GitObjectDbQuery query)
    {
        var description = query.DtoEmitter.Model.GetDescription(typeof(TNode));
        foreach (var childType in description.Children)
        {
            var dtoEmitterInfo = query.DtoEmitter.TypeDescriptions.First(d => d.NodeType.Equals(childType));
            query.AddCollectionField(this, dtoEmitterInfo);
        }
    }
}
