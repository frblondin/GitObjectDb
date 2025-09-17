using GitDotNet;
using GitObjectDb.Api.GraphQL.Graph;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL.Queries;

internal class HistoryResolver : IFieldResolver
{
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        var connection = context.RequestServices?.GetRequiredService<IConnection>() ??
           throw new RequestError("No connection context set.");
        var commits = connection.Repository.GetLogAsync(
            context.GetArgument<string>(Query.HistoryEndCommit),
            new(SortBy: LogTraversal.Topological | LogTraversal.FirstParentOnly,
            ExcludeReachableFrom: context.GetArgument<string>(Query.HistoryStartCommit)))
            .SelectAwait(async entry => await entry.GetCommitAsync());
        return ValueTask.FromResult((object?)commits.ToEnumerable().ToList());
    }
}