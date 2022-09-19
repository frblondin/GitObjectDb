using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Api.GraphQL.Tools;
using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL.Queries;
internal static class NodeQuery
{
    internal static FuncFieldResolver<object?, object?> CreateResolver(TypeDescription description)
    {
        var method = ExpressionReflector.GetMethod(() => Execute<Node, NodeDto>(default!), returnGenericDefinition: true);
        return new(
            method.MakeGenericMethod(description.NodeType.Type, description.DtoType)
            .CreateDelegate<Func<IResolveFieldContext<object?>, object?>>());
    }

    private static IEnumerable<TNodeDto> Execute<TNode, TNodeDto>(IResolveFieldContext<object?> context)
        where TNode : Node
        where TNodeDto : NodeDto
    {
        var provider = context.RequestServices?.GetRequiredService<DataProvider>() ??
            throw new NotSupportedException("No request context set.");
        var parentNode = context.Source is NodeDto dto ? dto.Node : null;
        var parentPath = parentNode?.Path?.FilePath ?? context.GetArgument(GitObjectDbQuery.ParentPathArgument, default(string?));

        var committish = context.GetArgumentFromParentContexts(GitObjectDbQuery.CommittishArgument, default(string?));
        var isRecursive = context.GetArgumentFromParentContexts(GitObjectDbQuery.IsRecursiveArgument, false);

        var result = provider.GetNodes<TNode, TNodeDto>(parentPath, committish, isRecursive);

        var id = context.GetArgument(GitObjectDbQuery.IdArgument, default(string?));
        if (id != null)
        {
            result = result.Where(x => x.Id == id);
        }

        return result;
    }
}
