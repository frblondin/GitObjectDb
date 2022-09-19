using GitObjectDb.Api.Model;
using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.GraphModel;

public class GitObjectDbSchema : Schema
{
    public GitObjectDbSchema(GitObjectDbQuery query)
    {
        Query = query;

        RegisterTypeMapping(typeof(NodeDto), typeof(NodeInterface));
    }
}
