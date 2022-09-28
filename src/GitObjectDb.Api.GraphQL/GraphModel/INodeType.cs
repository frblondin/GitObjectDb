using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.GraphModel;

internal interface INodeType<in T> : IGraphType
{
    void AddFieldsThroughReflection(T graph);
}
