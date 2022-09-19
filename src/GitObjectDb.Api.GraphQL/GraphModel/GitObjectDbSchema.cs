using GitObjectDb.Api.Model;
using GitObjectDb.Model;
using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.GraphModel;

public class GitObjectDbSchema : Schema
{
    public GitObjectDbSchema(DtoTypeEmitter emitter)
    {
        AdditionalTypeMappings.Add(this);

        Query = new GitObjectDbQuery(emitter);
        Mutation = new GitObjectDbMutation((GitObjectDbQuery)Query);
    }
}
