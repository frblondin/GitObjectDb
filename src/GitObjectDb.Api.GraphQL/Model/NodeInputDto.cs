namespace GitObjectDb.Api.GraphQL.Model;

/// <summary>Represents the base class for node input data transfer objects.</summary>
public abstract class NodeInputDto
{
}

/// <summary>Represents the base class for node input data transfer objects with a specific node type.</summary>
/// <typeparam name="TNode">The type of the node.</typeparam>
#pragma warning disable SA1402 // File may only contain a single type
public abstract class NodeInputDto<TNode> : NodeInputDto
{
}