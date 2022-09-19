using Fasterflect;
using GitObjectDb.Api.GraphQL.Queries;
using GitObjectDb.Api.Model;
using GitObjectDb.Model;
using GraphQL.Types;
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

    private readonly Dictionary<DataTransferTypeDescription, INodeType> _typeToGraphType = new();
    private readonly Dictionary<DataTransferTypeDescription, INodeDeltaType> _typeToDeltaGraphType = new();

    public GitObjectDbQuery(DtoTypeEmitter emitter)
    {
        Name = "Query";
        Description = "Provides queries to access GitObjectDb items.";
        DtoEmitter = emitter;

        foreach (var description in DtoEmitter.TypeDescriptions)
        {
            AddCollectionField(this, description);
            AddCollectionDeltaField(this, description);
        }
    }

    public DtoTypeEmitter DtoEmitter { get; }

    internal IGraphType GetOrCreateGraphType(Type dtoType, out Type nodeType)
    {
        if (dtoType == typeof(NodeDto))
        {
            nodeType = typeof(Node);
            return new NodeInterface();
        }
        else
        {
            var description = DtoEmitter.TypeDescriptions.First(d => d.DtoType == dtoType);
            nodeType = description.NodeType.Type;
            return GetOrCreateNodeGraphType(description);
        }
    }

    internal INodeType GetOrCreateNodeGraphType(DataTransferTypeDescription description)
    {
        if (!_typeToGraphType.TryGetValue(description, out var result))
        {
            var schemaType = typeof(NodeType<,>).MakeGenericType(description.NodeType.Type, description.DtoType);
            var factory = Reflect.Constructor(schemaType);
            _typeToGraphType[description] = result = (INodeType)factory.Invoke();

            result.AddReferences(this);
            result.AddChildren(this);
        }
        return result;
    }

    internal IGraphType GetOrCreateNodeDeltaGraphType(DataTransferTypeDescription description)
    {
        if (!_typeToDeltaGraphType.TryGetValue(description, out var result))
        {
            var schemaType = typeof(NodeDeltaType<,>).MakeGenericType(description.NodeType.Type, description.DtoType);
            var factory = Reflect.Constructor(schemaType);
            _typeToDeltaGraphType[description] = result = (INodeDeltaType)factory.Invoke();

            result.AddNodeReference(this);
        }
        return result;
    }

    internal FieldType AddCollectionField(IComplexGraphType graphType, DataTransferTypeDescription description)
    {
        var childGraphType = GetOrCreateNodeGraphType(description);

        return graphType.AddField(new()
        {
            Name = description.NodeType.Name,
            Description = description.NodeType.Type.GetXmlDocsSummary(false),
            Type = typeof(ListGraphType<>).MakeGenericType(childGraphType.GetType()),
            ResolvedType = new ListGraphType(childGraphType),
            Arguments = new(
                new QueryArgument<StringGraphType> { Name = IdArgument, Description = "Id of requested node." },
                new QueryArgument<StringGraphType> { Name = ParentPathArgument, Description = "Parent of the nodes." },
                new QueryArgument<StringGraphType> { Name = CommittishArgument, Description = "Optional committish (head tip is used otherwise)." },
                new QueryArgument<BooleanGraphType> { Name = IsRecursiveArgument, Description = "Whether all nested nodes should be flattened." }),
            Resolver = NodeQuery.CreateResolver(description),
        });
    }

    internal FieldType AddCollectionDeltaField(IComplexGraphType graphType, DataTransferTypeDescription description)
    {
        var deltaGraphType = GetOrCreateNodeDeltaGraphType(description);

        return graphType.AddField(new()
        {
            Name = $"{description.NodeType.Name}Delta",
            Description = $"Performs delta requests for {description.NodeType.Name}.",
            Type = typeof(ListGraphType<>).MakeGenericType(deltaGraphType.GetType()),
            ResolvedType = new ListGraphType(deltaGraphType),
            Arguments = new(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = DeltaStartCommit, Description = "Start committish of comparison." },
                new QueryArgument<StringGraphType> { Name = DeltaEndCommit, Description = "Optional end committish (head tip is used otherwise)." }),
            Resolver = NodeDeltaQuery.CreateResolver(description),
        });
    }
}
