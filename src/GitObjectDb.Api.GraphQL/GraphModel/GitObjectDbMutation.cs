using Fasterflect;
using GitObjectDb.Api.GraphQL.Commands;
using GitObjectDb.Api.GraphQL.Converters;
using GitObjectDb.Model;
using GraphQL;
using GraphQL.Types;
using Models.Organization;

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

    public GitObjectDbMutation(GitObjectDbSchema schema)
    {
        Name = "Mutation";
        Description = "Mutates GitObjectDb data.";
        Schema = schema;

        foreach (var description in schema.Model.NodeTypes)
        {
            AddNodeField(description);
        }
        Field<StringGraphType>("Checkout")
            .Description("Checkout a branch.")
            .Arguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = BranchArgument })
            .Resolve(NodeMutation.Checkout);
        Field<DataPathGraphType>("DeleteNode")
            .Description("Delete an existing node.")
            .Arguments(
                new QueryArgument<NonNullGraphType<DataPathGraphType>> { Name = PathArgument })
            .Resolve(NodeMutation.Delete);
        Field<ObjectIdGraphType>("Commit")
            .Description("Commit all previous changes.")
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
            .Description($"Creates {description.Name}.")
            .Arguments(
                new QueryArgument(typeof(NonNullGraphType<>).MakeGenericType(inputType.GetType()))
                {
                    Name = NodeArgument,
                    ResolvedType = inputType,
                },
                new QueryArgument<DataPathGraphType> { Name = ParentPathArgument },
                new QueryArgument<UniqueIdGraphType> { Name = ParentIdArgument })
            .Resolve(NodeMutation.CreateAddOrUpdate(description));

        // Registers the transformation of a path to a node
        ValueConverter.Register(typeof(string), description.Type, PathToNodeConverter.Convert);
    }

    internal INodeType<GitObjectDbMutation> GetOrCreateNodeGraphType(NodeTypeDescription description)
    {
        if (!_typeToGraphType.TryGetValue(description, out var result))
        {
            var schemaType = typeof(NodeInputType<>).MakeGenericType(description.Type);
            var factory = Reflect.Constructor(schemaType);
            _typeToGraphType[description] = result = (INodeType<GitObjectDbMutation>)factory.Invoke();

            result.AddFieldsThroughReflection(this);
        }
        return result;
    }
}
