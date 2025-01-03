using Fasterflect;
using GitObjectDb.Api.GraphQL.Graph.Objects;
using GitObjectDb.Api.GraphQL.Graph.Scalars;
using GitObjectDb.Api.GraphQL.Mutations;
using GitObjectDb.Api.GraphQL.Queries;
using GitObjectDb.Api.GraphQL.Tools;
using GitObjectDb.Model;
using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.Graph;

internal class Mutation : ObjectGraphType
{
    internal const string BranchArgument = "branch";
    internal const string NodeArgument = "node";
    internal const string ParentPathArgument = "parentPath";
    internal const string ParentIdArgument = "parentId";
    internal const string PathArgument = "path";
    internal const string AuthorArgument = "author";
    internal const string EMailArgument = "email";
    internal const string MessageArgument = "message";

    private readonly Dictionary<NodeTypeDescription, INodeType<Mutation>> _typeToGraphType = new();
    private readonly NodeInputDtoTypeEmitter _dtoTypeEmitter;

    public Mutation(Schema schema, NodeInputDtoTypeEmitter dtoTypeEmitter)
    {
        Name = "Mutation";
        Description = "Mutates GitObjectDb data.";
        Schema = schema;
        _dtoTypeEmitter = dtoTypeEmitter;

        foreach (var description in schema.Model.NodeTypes)
        {
            AddNodeField(description);
        }
        Field<StringGraphType>("Checkout")
            .Description("Checkouts an existing branch from its name.")
            .Arguments(
                NewArg<NonNullGraphType<StringGraphType>>(BranchArgument, "The name of the branch to checkout."))
            .ResolveThroughDI().UsingResolver<CheckoutMutation>();
        Field<DataPathGraphType>("DeleteNode")
            .Description("Deletes an existing node from its path.")
            .Arguments(
                NewArg<NonNullGraphType<DataPathGraphType>>(PathArgument, "The path of the node to delete."))
            .ResolveThroughDI().UsingResolver<DeleteMutation>();
        Field<ObjectIdGraphType>("Commit")
            .Description("Commits all previous changes.")
            .Arguments(
                NewArg<NonNullGraphType<StringGraphType>>(MessageArgument, "The commit message."),
                NewArg<NonNullGraphType<StringGraphType>>(AuthorArgument, "The author of the commit."),
                NewArg<NonNullGraphType<StringGraphType>>(EMailArgument, "The email of the author."))
            .ResolveThroughDI().UsingResolver<CommitMutation>();
    }

    public Schema Schema { get; }

    internal void AddNodeField(NodeTypeDescription description)
    {
        var inputType = GetOrCreateNodeGraphType(description);

        Field<DataPathGraphType>($"Create{description.Type.Name}")
            .Description($"Creates or updates a {description.Name} node in repository.")
            .Arguments(
                new QueryArgument(typeof(NonNullGraphType<>).MakeGenericType(inputType.GetType()))
                {
                    Name = NodeArgument,
                    ResolvedType = inputType,
                    Description = $"The node to create or update.",
                },
                NewArg<DataPathGraphType>(ParentPathArgument, "The path of the parent node."),
                NewArg<UniqueIdGraphType>(ParentIdArgument, "The id of the parent node."))
            .Resolve(AddOrUpdateMutation.For(description));
    }

    internal INodeType<Mutation> GetOrCreateNodeGraphType(NodeTypeDescription description)
    {
        if (!_typeToGraphType.TryGetValue(description, out var result))
        {
            var dto = _dtoTypeEmitter.TypeToInputDto[description.Type].AsType();
            var schemaType = typeof(NodeInputType<>).MakeGenericType(dto);
            var factory = Reflect.Constructor(schemaType);
            _typeToGraphType[description] = result = (INodeType<Mutation>)factory.Invoke();

            // Add fields outside constructor to avoid call overflows when there are
            // circular type references
            result.AddFieldsThroughReflection(this);
        }
        return result;
    }

    private static QueryArgument<TType> NewArg<TType>(string name, string description)
        where TType : IGraphType =>
        new() { Name = name, Description = description };
}
