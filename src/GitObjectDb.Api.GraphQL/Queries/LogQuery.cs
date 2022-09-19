using AutoMapper;
using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Api.Model;
using GraphQL;
using LibGit2Sharp;

namespace GitObjectDb.Api.GraphQL.Queries;

internal static class LogQuery
{
    internal static IEnumerable<Commit> Execute(object? source, IResolveFieldContext context, DataProvider provider, IMapper mapper)
    {
        var parentNode = source is NodeDto dto ? dto.Node : null;
        var branch = context.GetArgument(GitObjectDbQuery.BranchArgument, default(string?));

        return parentNode?.Path is null ?
            Enumerable.Empty<Commit>() :
            provider.QueryAccessor
                .GetCommits(parentNode, branch)
                .Select(e => e.Commit);
    }
}
