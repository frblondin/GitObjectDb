using Fasterflect;
using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.Model;

public partial class GitObjectDbQuery : ObjectGraphType
{
    private const string IdArgument = "id";
    private const string ParentPathArgument = "parentPath";
    private const string CommittishArgument = "committish";
    private const string IsRecursiveArgument = "isRecursive";

    internal static FieldType AddCollectionField(
        GitObjectDbQuery query, IComplexGraphType graphType, TypeDescription description)
    {
        var nodeSchemaType = typeof(NodeType<,>).MakeGenericType(description.NodeType.Type, description.DtoType);
        var type = typeof(ListGraphType<>).MakeGenericType(nodeSchemaType);
        var schemaTypeInvoker = Reflect.Constructor(nodeSchemaType, typeof(GitObjectDbQuery));
        var nodeResolver = CreateNodeResolver(description.NodeType.Type, description.DtoType);
        return graphType.AddField(new FieldType
        {
            Name = description.NodeType.Name,
            Type = type,
            ResolvedType = new ListGraphType((IGraphType?)schemaTypeInvoker.Invoke(query)),
            Arguments = new QueryArguments(
                new QueryArgument<StringGraphType> { Name = IdArgument, Description = "Id of requested node." },
                new QueryArgument<StringGraphType> { Name = ParentPathArgument, Description = "Parent of the nodes." },
                new QueryArgument<StringGraphType> { Name = CommittishArgument },
                new QueryArgument<BooleanGraphType> { Name = IsRecursiveArgument }),
            Resolver = new FuncFieldResolver<object?, object?>(nodeResolver),
        });
    }

    internal static Func<IResolveFieldContext<object?>, object?> CreateNodeResolver(Type nodeType, Type dtoType) =>
        typeof(GitObjectDbQuery)
        .GetMethod("QueryNodes", BindingFlags.Static | BindingFlags.NonPublic)!
        .MakeGenericMethod(nodeType, dtoType)
        .CreateDelegate<Func<IResolveFieldContext<object?>, object?>>();

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used through reflection")]
    private static IEnumerable<TNodeDTO> QueryNodes<TNode, TNodeDTO>(IResolveFieldContext<object?> context)
        where TNode : Node
        where TNodeDTO : NodeDto
    {
        var provider = context.RequestServices?.GetRequiredService<DataProvider>() ??
            throw new NotSupportedException("No request context set.");
        var parentNode = context.Source is NodeDto dto ? dto.Node : null;

        var parentPath = parentNode?.Path?.FilePath ?? context.GetArgument(ParentPathArgument, default(string?));
        var committish = context.GetArgument(CommittishArgument, default(string?));
        var isRecursive = context.GetArgument(IsRecursiveArgument, false);

        var result = provider.GetNodes<TNode, TNodeDTO>(parentPath, committish, isRecursive);

        var id = context.GetArgument(IdArgument, default(string?));
        if (id != null)
        {
            result = result.Where(x => x.Id == id);
        }

        return result;
    }
}
