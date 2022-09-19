using GitObjectDb.Api.Model;
using GraphQL;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL.Model;

public partial class GitObjectDbQuery
{
    internal static IEnumerable<Commit> QueryLog(IResolveFieldContext<object?> context)
    {
        var provider = context.RequestServices?.GetRequiredService<DataProvider>() ??
            throw new NotSupportedException("No request context set.");
        var parentNode = context.Source is NodeDto dto ? dto.Node : null;
        var branch = context.GetArgument(BranchArgument, default(string?));

        return parentNode?.Path is null ?
            Enumerable.Empty<Commit>() :
            provider.QueryAccessor
                .GetCommits(parentNode, branch)
                .Select(e => e.Commit);
    }
}
