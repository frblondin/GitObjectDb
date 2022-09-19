using AutoMapper;
using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Api.GraphQL.Tools;
using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL.Queries;
internal static class NodeDeltaQuery
{
    internal static FuncFieldResolver<object?, object?> CreateResolver(TypeDescription description)
    {
        var method = ExpressionReflector.GetMethod(() => Execute<Node, NodeDto>(default!), returnGenericDefinition: true);
        return new(
            method.MakeGenericMethod(description.NodeType.Type, description.DtoType)
            .CreateDelegate<Func<IResolveFieldContext<object?>, object?>>());
    }

    private static IEnumerable<DeltaDto<TNodeDto>> Execute<TNode, TNodeDto>(IResolveFieldContext<object?> context)
        where TNode : Node
        where TNodeDto : NodeDto
    {
        var provider = context.RequestServices?.GetRequiredService<DataProvider>() ??
            throw new NotSupportedException("No request context set.");

        var start = context.GetArgument<string>(GitObjectDbQuery.DeltaStartCommit);
        var end = context.GetArgument(GitObjectDbQuery.DeltaEndCommit, default(string?));

        return provider.GetNodeDeltas<TNode, TNodeDto>(start, end);
    }
}
