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
        var parentNode = context.Source is NodeDTO dto ? dto.Node : null;

        if (parentNode?.Path is null)
        {
            return Enumerable.Empty<Commit>();
        }
        else
        {
            return provider.Connection.Repository.Commits
                .QueryBy(parentNode.Path.FilePath)
                .Select(e => e.Commit);
        }
    }
}
