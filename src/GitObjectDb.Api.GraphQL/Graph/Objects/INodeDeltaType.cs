using GitObjectDb.Api.GraphQL.Graph;
using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.Graph.Objects;

internal interface INodeDeltaType : IGraphType
{
    void AddNodeReference(Query query);
}
