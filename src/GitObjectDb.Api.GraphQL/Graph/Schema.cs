using GitObjectDb.Api.GraphQL.Graph.Objects;
using GitObjectDb.Api.GraphQL.Graph.Scalars;
using GitObjectDb.Model;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.Options;

namespace GitObjectDb.Api.GraphQL.Graph;

/// <summary>Represents the GraphQL schema for the GitObjectDb API.</summary>
public class Schema : global::GraphQL.Types.Schema
{
    /// <summary>Initializes a new instance of the <see cref="Schema"/> class.</summary>
    /// <param name="model">The data model.</param>
    /// <param name="options">The GraphQL options.</param>
    /// <param name="dtoTypeEmitter">The DTO type emitter.</param>
    public Schema(IDataModel model, IOptions<GitObjectDbGraphQLOptions> options, NodeInputDtoTypeEmitter dtoTypeEmitter)
    {
        Model = model;

        ScalarTypes.Add(this);
        this.RegisterTypeMapping<Node, NodeInterfaceType>();

        options.Value.ConfigureSchema?.Invoke(this);

        Query = new Query(this);
        Mutation = new Mutation(this, dtoTypeEmitter);
    }

    /// <summary>Gets the data model.</summary>
    public IDataModel Model { get; }
}
