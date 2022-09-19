using Fasterflect;
using GitObjectDb.Api.GraphQL.Commands;
using GitObjectDb.Api.GraphQL.Converters;
using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.GraphModel;

public class GitObjectDbMutation : ObjectGraphType
{
    internal const string NodeArgument = "node";
    internal const string ParentPathArgument = "parentPath";
    internal const string ParentIdArgument = "parentId";
    internal const string PathArgument = "path";
    internal const string AuthorArgument = "author";
    internal const string EMailArgument = "email";
    internal const string MessageArgument = "message";

    private readonly GitObjectDbQuery _query;
    private readonly Dictionary<DataTransferTypeDescription, IInputObjectGraphType> _typeToGraphType = new();

    public GitObjectDbMutation(GitObjectDbQuery query)
    {
        Name = "Mutation";
        Description = "Mutates GitObjectDb data.";
        _query = query;
        foreach (var description in _query.DtoEmitter.TypeDescriptions)
        {
            AddNodeField(description);
        }
        Field<StringGraphType>("DeleteNode")
            .Description("Delete an existing node.")
            .Arguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = PathArgument })
            .Resolve(NodeMutation.Delete);
        Field<StringGraphType>("Commit")
            .Description("Commit all previous changes.")
            .Arguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = MessageArgument },
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = AuthorArgument },
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = EMailArgument })
            .Resolve(NodeMutation.Commit);
    }

    internal void AddNodeField(DataTransferTypeDescription description)
    {
        var inputType = GetOrCreateNodeGraphType(description);

        Field<StringGraphType>($"Create{description.NodeType.Type.Name}")
            .Description($"Creates {description.NodeType.Name}.")
            .Arguments(
                new QueryArgument(typeof(NonNullGraphType<>).MakeGenericType(inputType.GetType()))
                {
                    Name = NodeArgument,
                },
                new QueryArgument<StringGraphType> { Name = ParentPathArgument },
                new QueryArgument<StringGraphType> { Name = ParentIdArgument })
            .Resolve(NodeMutation.CreateAddOrUpdate(description));

        // Registers the transformation of a path to a dto
        ValueConverter.Register(typeof(string), description.DtoType, PathToNodeConverter.Convert);
    }

    internal IInputObjectGraphType GetOrCreateNodeGraphType(DataTransferTypeDescription description)
    {
        if (!_typeToGraphType.TryGetValue(description, out var result))
        {
            var schemaType = typeof(NodeInputType<,>).MakeGenericType(description.NodeType.Type, description.DtoType);
            var factory = Reflect.Constructor(schemaType);
            _typeToGraphType[description] = result = (IInputObjectGraphType)factory.Invoke();
        }
        return result;
    }
}
