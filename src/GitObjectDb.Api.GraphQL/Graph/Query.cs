using Fasterflect;
using GitObjectDb.Api.GraphQL.Graph.Objects;
using GitObjectDb.Api.GraphQL.Graph.Scalars;
using GitObjectDb.Api.GraphQL.Model;
using GitObjectDb.Api.GraphQL.Queries;
using GitObjectDb.Api.GraphQL.Tools;
using GitObjectDb.Model;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Execution;
using GraphQL.MicrosoftDI;
using GraphQL.Resolvers;
using GraphQL.Types;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Namotion.Reflection;

namespace GitObjectDb.Api.GraphQL.Graph;

/// <summary>
/// Represents the root query type for the GitObjectDb GraphQL API.
/// Provides queries to access GitObjectDb items.
/// </summary>
public partial class Query : ObjectGraphType
{
    internal const string IdArgument = "id";
    internal const string ParentPathArgument = "parentPath";
    internal const string CommittishArgument = "committish";
    internal const string BranchArgument = "branch";
    internal const string IsRecursiveArgument = "isRecursive";

    internal const string DeltaStartCommit = "start";
    internal const string DeltaEndCommit = "end";

    internal const string HistoryStartCommit = "start";
    internal const string HistoryEndCommit = "end";

    private readonly Dictionary<NodeTypeDescription, INodeType<Query>> _typeToGraphType = [];
    private readonly Dictionary<NodeTypeDescription, INodeDeltaType> _typeToDeltaGraphType = [];

    /// <summary>Initializes a new instance of the <see cref="Query"/> class.</summary>
    /// <param name="schema">The GraphQL schema for the GitObjectDb API.</param>
    public Query(Schema schema)
    {
        Name = "Query";
        Description = "Provides queries to access GitObjectDb items.";
        Schema = schema;

        foreach (var description in schema.Model.NodeTypes)
        {
            if (description.Type == typeof(Node))
            {
                continue;
            }
            AddNodeListField(this, description);
            AddNodeDeltaListField(this, description);
        }
        AddHistoryField();
    }

    /// <summary>Gets the GraphQL schema for the GitObjectDb API.</summary>
    public Schema Schema { get; }

    internal IGraphType GetOrCreateGraphType(Type nodeType) =>
        nodeType == typeof(Node) ?
        NodeInterfaceType.Instance :
        GetOrCreateNodeGraphType(Schema.Model.NodeTypes.First(d => d.Type == nodeType));

    private INodeType<Query> GetOrCreateNodeGraphType(NodeTypeDescription description)
    {
        if (!_typeToGraphType.TryGetValue(description, out var result))
        {
            var schemaType = typeof(NodeType<>).MakeGenericType(description.Type);
            var factory = Reflect.Constructor(schemaType);
            _typeToGraphType[description] = result = (INodeType<Query>)factory.Invoke();

            // Add fields outside constructor to avoid call overflows when there are
            // circular type references
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

    internal void AddNodeListField(IComplexGraphType graphType, NodeTypeDescription description) => graphType.AddField(
        FieldBuilder<object?, IEnumerable<Node>>
            .Create(description.Name, new ListGraphType(GetOrCreateGraphType(description.Type)))
            .Description(description.Type.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }))
            .Arguments(
                NewArg<NonNullGraphType<StringGraphType>>(CommittishArgument, "Committish containing requested nodes."),
                NewArg<UniqueIdGraphType>(IdArgument, "Id of requested node."),
                NewArg<DataPathGraphType>(ParentPathArgument, "Parent of the nodes."),
                NewArg<BooleanGraphType>(IsRecursiveArgument, "Whether all nested nodes should be flattened."))
            .ResolveThroughDI().UsingLoader<NodeDataLoaderKey>(typeof(NodeLoader<>).MakeGenericType(description.Type))
            .FieldType);

    internal void AddNodeDeltaListField(IComplexGraphType graphType, NodeTypeDescription description) => graphType.AddField(
        FieldBuilder<object?, IEnumerable<DeltaDto>>
            .Create($"{description.Name}Delta", new ListGraphType(GetOrCreateNodeDeltaGraphType(description)))
            .Description($"Performs delta requests for {description.Name}.")
            .Arguments(
                NewArg<NonNullGraphType<StringGraphType>>(DeltaStartCommit, "Start committish of comparison."),
                NewArg<NonNullGraphType<StringGraphType>>(DeltaEndCommit, "End committish of comparison."))
            .ResolveThroughDI().UsingLoader<NodeDeltaDataLoaderKey>(typeof(NodeDeltaLoader<>).MakeGenericType(description.Type))
            .FieldType);

    private void AddHistoryField() =>
        Field<ListGraphType<CommitType>, IEnumerable<Commit>>("History")
            .Description("Gets the history of changes in repository.")
            .Arguments(
            [
                NewArg<NonNullGraphType<StringGraphType>>(HistoryStartCommit, "Start committish of history lookup."),
                NewArg<NonNullGraphType<StringGraphType>>(HistoryEndCommit, "End committish of history lookup."),
            ])
            .ResolveThroughDI().UsingResolver<HistoryResolver>();

    private static QueryArgument<TType> NewArg<TType>(string name, string description)
        where TType : IGraphType =>
        new() { Name = name, Description = description };
}
