using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.GraphModel;

internal interface INodeDeltaType : IGraphType
{
    void AddNodeReference(GitObjectDbQuery query);
}
