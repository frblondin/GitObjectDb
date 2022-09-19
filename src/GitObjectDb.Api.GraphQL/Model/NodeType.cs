using AutoMapper;
using Fasterflect;
using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.Model;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name
internal interface INodeType : IGraphType
{
    void AddReferences(GitObjectDbQuery query);

    void AddChildren(GitObjectDbQuery query);
}

public sealed class NodeType<TNode, TNodeDto> : ObjectGraphType<TNodeDto>, INodeType
{
    public NodeType()
    {
        Name = typeof(TNode).Name.Replace("`", string.Empty);

        AddScalarProperties();

        AddField(new FieldType
        {
            Name = "History",
            Type = typeof(ListGraphType<CommitType>),
            Arguments = new(
                new QueryArgument<StringGraphType> { Name = GitObjectDbQuery.BranchArgument }),
            Resolver = new FuncFieldResolver<object?, object?>(GitObjectDbQuery.QueryLog),
        });

        Interface<NodeInterface>();
    }

    private void AddScalarProperties()
    {
        foreach (var property in typeof(TNodeDto).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!SchemaTypes.BuiltInScalarMappings.ContainsKey(property.PropertyType))
            {
                continue;
            }
            var type = property.PropertyType.GetGraphTypeFromType(isNullable: true, TypeMappingMode.OutputType);
            Field(type, property.Name);
        }
    }

    void INodeType.AddReferences(GitObjectDbQuery query)
    {
        foreach (var property in typeof(TNodeDto).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
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
        var type = query.GetOrCreateGraphType(property.PropertyType, out var _);

        AddField(new()
        {
            Name = property.Name,
            Type = type.GetType(),
            ResolvedType = type,
            Resolver = new FuncFieldResolver<object?, object?>(context =>
            {
                var parentNode = context.Source as NodeDto ??
                    throw new NotSupportedException("Could not get parent node.");
                var getter = Reflect.PropertyGetter(parentNode.Node.GetType(), property.Name);
                var reference = (Node)getter.Invoke(parentNode.Node);
                var mapper = context.RequestServices?.GetRequiredService<IMapper>() ??
                    throw new NotSupportedException("No mapper context set.");
                return mapper.Map(reference, property.PropertyType, property.PropertyType);
            }),
        });
    }

    private void AddMultiReference(GitObjectDbQuery query, MemberInfo member, Type dtoType)
    {
        var type = query.GetOrCreateGraphType(dtoType, out var nodeType);
        var sourceEnumType = typeof(IEnumerable<>).MakeGenericType(nodeType);
        var destEnumType = typeof(IEnumerable<>).MakeGenericType(dtoType);

        AddField(new()
        {
            Name = member.Name,
            Type = type.GetType(),
            ResolvedType = type,
            Resolver = new FuncFieldResolver<object?, object?>(context =>
            {
                var parentNode = context.Source as NodeDto ??
                    throw new NotSupportedException("Could not get parent node.");
                var getter = Reflect.PropertyGetter(parentNode.Node.GetType(), member.Name);
                var references = (Node)getter.Invoke(parentNode.Node);
                var mapper = context.RequestServices?.GetRequiredService<IMapper>() ??
                    throw new NotSupportedException("No mapper context set.");
                return mapper.Map(references,
                                  sourceEnumType,
                                  destEnumType);
            }),
        });
    }

    void INodeType.AddChildren(GitObjectDbQuery query)
    {
        var description = query.Model.GetDescription(typeof(TNode));
        foreach (var childType in description.Children)
        {
            var dtoEmitterInfo = query.DtoEmitter.TypeDescriptions.First(d => d.NodeType.Equals(childType));
            query.AddCollectionField(this, dtoEmitterInfo);
        }
    }
}
