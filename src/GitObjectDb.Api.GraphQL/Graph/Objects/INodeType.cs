using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.Graph.Objects;

internal interface INodeType<in T> : IGraphType
{
    void AddFieldsThroughReflection(T graph);
}
