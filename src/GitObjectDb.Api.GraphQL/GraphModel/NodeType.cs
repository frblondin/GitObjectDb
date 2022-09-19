using Fasterflect;
using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Resolvers;
using GraphQL.Types;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
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

    private void AddHistoryField() =>
        NodeInterface.CreateHistoryField(this)
        .Arguments(
            new QueryArgument<StringGraphType> { Name = GitObjectDbQuery.BranchArgument })
        .Resolve(context =>
        {
            var branch = context.GetArgument(GitObjectDbQuery.BranchArgument, default(string?));
            var provider = context.RequestServices?.GetRequiredService<DataProvider>() ??
                throw new RequestError("No request context set.");

            return context.Source.Path is null ?
                Enumerable.Empty<Commit>() :
                provider.QueryAccessor
                    .GetCommits(context.Source.Node!, branch)
                    .Select(e => e.Commit);
        });

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
        var getter = Reflect.PropertyGetter(property);

        Field(property.Name, type)
            .Description(typeof(TNode).GetProperty(property.Name)?.GetXmlDocsSummary(false) ??
                property.GetXmlDocsSummary(false))
            .Resolve(new FuncFieldResolver<object?, object?>(context =>
            {
                var parentNode = context.Source as NodeDto ??
                    throw new RequestError("Could not get parent node.");
                return getter.Invoke(parentNode);
            }));
    }

    private void AddMultiReference(GitObjectDbQuery query, PropertyInfo property, Type dtoType)
    {
        var type = query.GetOrCreateGraphType(dtoType, out var nodeType);
        var getter = Reflect.PropertyGetter(property);

        Field(property.Name, type)
            .Description(typeof(TNode).GetProperty(property.Name)?.GetXmlDocsSummary(false) ??
                property.GetXmlDocsSummary(false))
            .Resolve(new FuncFieldResolver<object?, object?>(context =>
            {
                var parentNode = context.Source as NodeDto ??
                    throw new RequestError("Could not get parent node.");
                return getter.Invoke(parentNode);
            }));
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
