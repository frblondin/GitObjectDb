using GitObjectDb.Api.GraphQL.Graph;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Resolvers;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL.Queries;

internal class HistoryResolver : IFieldResolver
{
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        var connection = context.RequestServices?.GetRequiredService<IConnection>() ??
           throw new RequestError("No connection context set.");
        return ValueTask.FromResult((object?)connection.Repository.Commits.QueryBy(new CommitFilter
        {
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
            ExcludeReachableFrom = context.GetArgument<string>(Query.HistoryStartCommit),
            IncludeReachableFrom = context.GetArgument<string>(Query.HistoryEndCommit),
        }));
    }
}