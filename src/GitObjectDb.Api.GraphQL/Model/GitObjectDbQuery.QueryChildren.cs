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

    internal FieldType AddCollectionField(IComplexGraphType graphType, TypeDescription description)
    {
        var childGraphType = GetOrCreateGraphType(description);
        var type = typeof(ListGraphType<>).MakeGenericType(childGraphType.GetType());
        var resolver = typeof(GitObjectDbQuery)
            .GetMethod("QueryNodes", BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(description.NodeType.Type, description.DtoType)
            .CreateDelegate<Func<IResolveFieldContext<object?>, object?>>();

        return graphType.AddField(new()
        {
            Name = description.NodeType.Name,
            Type = type,
            ResolvedType = new ListGraphType((IGraphType?)childGraphType),
            Arguments = new(
                new QueryArgument<StringGraphType> { Name = IdArgument, Description = "Id of requested node." },
                new QueryArgument<StringGraphType> { Name = ParentPathArgument, Description = "Parent of the nodes." },
                new QueryArgument<StringGraphType> { Name = CommittishArgument },
                new QueryArgument<BooleanGraphType> { Name = IsRecursiveArgument }),
            Resolver = new FuncFieldResolver<object?, object?>(resolver),
        });
    }

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
