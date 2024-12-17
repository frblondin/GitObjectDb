using GitObjectDb.Model;
using GraphQL.Introspection;
using GraphQL.Types;
using Microsoft.Extensions.Options;

namespace GitObjectDb.Api.GraphQL.GraphModel;

public class GitObjectDbSchema : Schema
{
    public GitObjectDbSchema(IDataModel model, IOptions<GitObjectDbGraphQLOptions> options, NodeInputDtoTypeEmitter dtoTypeEmitter)
    {
        Model = model;

        AdditionalTypeMappings.Add(this);
        options.Value.ConfigureSchema?.Invoke(this);

        Query = new GitObjectDbQuery(this);
        Mutation = new GitObjectDbMutation(this, dtoTypeEmitter);
    }

    public IDataModel Model { get; }
}
