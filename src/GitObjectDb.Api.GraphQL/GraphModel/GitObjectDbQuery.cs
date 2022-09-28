using Fasterflect;
using GitObjectDb.Api.GraphQL.Loaders;
using GitObjectDb.Model;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Introspection;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Models.Organization;
using Namotion.Reflection;

namespace GitObjectDb.Api.GraphQL.GraphModel;

public partial class GitObjectDbQuery : ObjectGraphType
{
    internal const string IdArgument = "id";
    internal const string ParentPathArgument = "parentPath";
    internal const string CommittishArgument = "committish";
    internal const string BranchArgument = "branch";
    internal const string IsRecursiveArgument = "isRecursive";

    internal const string DeltaStartCommit = "start";
    internal const string DeltaEndCommit = "end";

    private readonly Dictionary<NodeTypeDescription, INodeType<GitObjectDbQuery>> _typeToGraphType = new();
    private readonly Dictionary<NodeTypeDescription, INodeDeltaType> _typeToDeltaGraphType = new();

    public GitObjectDbQuery(GitObjectDbSchema schema)
    {
        Name = "Query";
        Description = "Provides queries to access GitObjectDb items.";
        Schema = schema;

        foreach (var description in schema.Model.NodeTypes)
        {
            AddCollectionField(this, description.Type, description.Name);
            AddCollectionDeltaField(this, description);
        }
    }

    public GitObjectDbSchema Schema { get; }

    internal IGraphType GetOrCreateGraphType(Type nodeType)
    {
        if (nodeType == typeof(Node))
        {
            return new NodeInterface();
        }
        else
        {
            var description = Schema.Model.NodeTypes.First(d => d.Type == nodeType);
            return GetOrCreateNodeGraphType(description);
        }
    }

    internal INodeType<GitObjectDbQuery> GetOrCreateNodeGraphType(NodeTypeDescription description)
    {
        if (!_typeToGraphType.TryGetValue(description, out var result))
        {
            var schemaType = typeof(NodeType<>).MakeGenericType(description.Type);
            var factory = Reflect.Constructor(schemaType);
            _typeToGraphType[description] = result = (INodeType<GitObjectDbQuery>)factory.Invoke();

            result.AddFieldsThroughReflection(this);
        }
        return result;
    }

    internal IGraphType GetOrCreateNodeDeltaGraphType(NodeTypeDescription description)
    {
        if (!_typeToDeltaGraphType.TryGetValue(description, out var result))
        {
            var schemaType = typeof(NodeDeltaType<>).MakeGenericType(description.Type);
            var factory = Reflect.Constructor(schemaType);
            _typeToDeltaGraphType[description] = result = (INodeDeltaType)factory.Invoke();

            result.AddNodeReference(this);
        }
        return result;
    }

    internal FieldType AddCollectionField(IComplexGraphType graphType, Type type, string name)
    {
        var childGraphType = GetOrCreateGraphType(type);
        var loaderType = typeof(NodeDataLoader<>).MakeGenericType(type);

        return graphType.AddField(new()
        {
            Name = name,
            Description = type.GetXmlDocsSummary(false),
            Type = typeof(ListGraphType<>).MakeGenericType(childGraphType.GetType()),
            ResolvedType = new ListGraphType(childGraphType),
            Arguments = new(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = CommittishArgument, Description = "Committish containing requested nodes." },
                new QueryArgument<UniqueIdGraphType> { Name = IdArgument, Description = "Id of requested node." },
                new QueryArgument<DataPathGraphType> { Name = ParentPathArgument, Description = "Parent of the nodes." },
                new QueryArgument<BooleanGraphType> { Name = IsRecursiveArgument, Description = "Whether all nested nodes should be flattened." }),
            Resolver = new FuncFieldResolver<object?, object?>(context =>
            {
                var loader = context.RequestServices?.GetRequiredService(loaderType) as DataLoaderBase<NodeDataLoaderKey, IEnumerable<Node>> ??
                    throw new ExecutionError("No request context set.");
                return loader.LoadAsync(new NodeDataLoaderKey(context));
            }),
        });
    }

    internal FieldType AddCollectionDeltaField(IComplexGraphType graphType, NodeTypeDescription description)
    {
        var deltaGraphType = GetOrCreateNodeDeltaGraphType(description);
        var loaderType = typeof(NodeDeltaDataLoader<>).MakeGenericType(description.Type);

        return graphType.AddField(new()
        {
            Name = $"{description.Name}Delta",
            Description = $"Performs delta requests for {description.Name}.",
            Type = typeof(ListGraphType<>).MakeGenericType(deltaGraphType.GetType()),
            ResolvedType = new ListGraphType(deltaGraphType),
            Arguments = new(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = DeltaStartCommit, Description = "Start committish of comparison." },
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = DeltaEndCommit, Description = "End committish of comparison." }),
            Resolver = new FuncFieldResolver<object?, object?>(context =>
            {
                var loader = context.RequestServices?.GetRequiredService(loaderType) as DataLoaderBase<NodeDeltaDataLoaderKey, IEnumerable<object?>> ??
                    throw new ExecutionError("No request context set.");
                return loader.LoadAsync(new NodeDeltaDataLoaderKey(context));
            }),
        });
    }
}
