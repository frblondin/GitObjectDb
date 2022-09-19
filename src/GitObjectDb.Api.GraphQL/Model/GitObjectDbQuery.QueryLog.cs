using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Types;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL.Model;

public partial class GitObjectDbQuery : ObjectGraphType
{
    internal static IEnumerable<Commit> QueryLog(IResolveFieldContext<object?> context)
    {
        var provider = context.RequestServices?.GetRequiredService<DataProvider>() ??
            throw new NotSupportedException("No request context set.");
        var parentNode = context.Source is NodeDto dto ? dto.Node : null;

        return parentNode?.Path is null ?
            Enumerable.Empty<Commit>() :
            provider.Connection.Repository.Commits
                .QueryBy(parentNode.Path.FilePath)
                .Select(e => e.Commit);
    }
}
