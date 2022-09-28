using Fasterflect;
using GitObjectDb.Api.GraphQL.Loaders;
using GitObjectDb.Api.GraphQL.Tools;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Resolvers;
using GraphQL.Types;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Namotion.Reflection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.GraphModel;

public class NodeType<TNode> : ObjectGraphType<TNode>, INodeType<GitObjectDbQuery>
    where TNode : Node
{
    public NodeType()
    {
        Name = typeof(TNode).Name.Replace("`", string.Empty);
        Description = typeof(TNode).GetXmlDocsSummary(false);

        Interface<NodeInterface>();

        AddChildrenField();
        AddHistoryField();
    }

    private void AddChildrenField() =>
        NodeInterface.CreateChildrenField(this)
        .Resolve(context =>
        {
            var loader = context.RequestServices?.GetRequiredService<NodeDataLoader<Node>>() ??
                throw new ExecutionError("No request context set.");
            return loader.LoadAsync(new NodeDataLoaderKey(context));
        });

    private void AddHistoryField() =>
        NodeInterface.CreateHistoryField(this)
        .Resolve(context =>
        {
            var commitId = context.GetCommitId();
            var queryAccessor = context.RequestServices?.GetRequiredService<IQueryAccessor>() ??
                throw new RequestError("No request context set.");

            return context.Source.Path is null ?
                Enumerable.Empty<Commit>() :
                queryAccessor
                    .GetCommits(commitId.Sha, context.Source!)
                    .Select(e => e.Commit);
        });

    void INodeType<GitObjectDbQuery>.AddFieldsThroughReflection(GitObjectDbQuery query)
    {
        AddScalarProperties(query);
        AddReferences(query);
        AddChildren(query);
    }

    private void AddScalarProperties(GitObjectDbQuery query)
    {
        foreach (var property in typeof(TNode).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.PropertyType.IsNode() ||
                property.PropertyType.IsNodeEnumerable(out var _) ||
                !property.PropertyType.IsValidClrTypeForGraph(query.Schema))
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

    private void AddReferences(GitObjectDbQuery query)
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

    private void AddSingleReference(GitObjectDbQuery query, PropertyInfo property)
    {
        var type = query.GetOrCreateGraphType(property.PropertyType);
        var getter = Reflect.PropertyGetter(property);

        Field(property.Name, type)
            .Description(property.GetXmlDocsSummary(false) ??
                property.GetXmlDocsSummary(false))
            .Resolve(new FuncFieldResolver<object?, object?>(context =>
            {
                var parentNode = context.Source as Node ??
                    throw new RequestError("Could not get parent node.");
                return getter.Invoke(parentNode);
            }));
    }

    private void AddMultiReference(GitObjectDbQuery query, PropertyInfo property, Type nodeType)
    {
        var type = query.GetOrCreateGraphType(nodeType);
        var getter = Reflect.PropertyGetter(property);

        Field(property.Name, type)
            .Description(property.GetXmlDocsSummary(false) ??
                property.GetXmlDocsSummary(false))
            .Resolve(new FuncFieldResolver<object?, object?>(context =>
            {
                var parentNode = context.Source as Node ??
                    throw new RequestError("Could not get parent node.");
                return getter.Invoke(parentNode);
            }));
    }

    private void AddChildren(GitObjectDbQuery query)
    {
        var description = query.Schema.Model.GetDescription(typeof(TNode));
        foreach (var childType in description.Children)
        {
            query.AddCollectionField(this, childType.Type, childType.Name);
        }
    }
}
