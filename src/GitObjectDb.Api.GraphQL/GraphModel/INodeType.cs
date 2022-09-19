using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.GraphModel;

internal interface INodeType : IGraphType
{
    void AddReferences(GitObjectDbQuery query);

    void AddChildren(GitObjectDbQuery query);
}
