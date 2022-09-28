using GitObjectDb.Model;
using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.GraphModel;

public class GitObjectDbSchema : Schema
{
    public GitObjectDbSchema(IDataModel model)
    {
        Model = model;
    }

    public IDataModel Model { get; }
}
