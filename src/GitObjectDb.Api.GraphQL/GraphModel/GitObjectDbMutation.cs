using Fasterflect;
using GitObjectDb.Api.GraphQL.Commands;
using GitObjectDb.Model;
using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.GraphModel;

public class GitObjectDbMutation : ObjectGraphType
{
    internal const string BranchArgument = "branch";
    internal const string NodeArgument = "node";
    internal const string ParentPathArgument = "parentPath";
    internal const string ParentIdArgument = "parentId";
    internal const string PathArgument = "path";
    internal const string AuthorArgument = "author";
    internal const string EMailArgument = "email";
    internal const string MessageArgument = "message";

    private readonly Dictionary<NodeTypeDescription, INodeType<GitObjectDbMutation>> _typeToGraphType = new();
    private readonly NodeInputDtoTypeEmitter _dtoTypeEmitter;

    public GitObjectDbMutation(GitObjectDbSchema schema, NodeInputDtoTypeEmitter dtoTypeEmitter)
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
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = BranchArgument })
            .Resolve(NodeMutation.Checkout);
        Field<DataPathGraphType>("DeleteNode")
            .Description("Deletes an existing node from its path.")
            .Arguments(
                new QueryArgument<NonNullGraphType<DataPathGraphType>> { Name = PathArgument })
            .Resolve(NodeMutation.Delete);
        Field<ObjectIdGraphType>("Commit")
            .Description("Commits all previous changes.")
            .Arguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = MessageArgument },
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = AuthorArgument },
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = EMailArgument })
            .Resolve(NodeMutation.Commit);
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
                },
                new QueryArgument<DataPathGraphType> { Name = ParentPathArgument },
                new QueryArgument<UniqueIdGraphType> { Name = ParentIdArgument })
            .Resolve(NodeMutation.CreateAddOrUpdate(description, inputType.GetType().GetGenericArguments()[0]));
    }

    internal INodeType<GitObjectDbMutation> GetOrCreateNodeGraphType(NodeTypeDescription description)
    {
        if (!_typeToGraphType.TryGetValue(description, out var result))
        {
            var dto = _dtoTypeEmitter.TypeToInputDto[description.Type].AsType();
            var schemaType = typeof(NodeInputType<>).MakeGenericType(dto);
            var factory = Reflect.Constructor(schemaType);
            _typeToGraphType[description] = result = (INodeType<GitObjectDbMutation>)factory.Invoke();

            result.AddFieldsThroughReflection(this);
        }
        return result;
    }
}
