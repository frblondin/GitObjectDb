using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.Model;

public class GitObjectDbSchema : Schema
{
    public GitObjectDbSchema(GitObjectDbQuery query)
    {
        Query = query;
    }
}
