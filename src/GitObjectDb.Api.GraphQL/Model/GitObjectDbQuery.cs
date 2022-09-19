using GitObjectDb.Api.Model;
using GitObjectDb.Model;
using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.Model;

public partial class GitObjectDbQuery : ObjectGraphType
{
    public GitObjectDbQuery(IDataModel model)
    {
        Model = model;

        Name = "Query";

        DtoEmitter = new DtoTypeEmitter(Model);

        foreach (var description in DtoEmitter.TypeDescriptions)
        {
            AddCollectionField(this, this, description);
        }
    }

    public DtoTypeEmitter DtoEmitter { get; }

    public IDataModel Model { get; }
}
