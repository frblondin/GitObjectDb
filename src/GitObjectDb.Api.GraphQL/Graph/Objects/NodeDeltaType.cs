using GitObjectDb.Api.GraphQL.Graph;
using GitObjectDb.Api.GraphQL.Graph.Scalars;
using GitObjectDb.Api.GraphQL.Model;
using GitObjectDb.Api.GraphQL.Tools;
using GraphQL.Types;
using Namotion.Reflection;

namespace GitObjectDb.Api.GraphQL.Graph.Objects;

/// <summary>Represents a GraphQL type for node deltas.</summary>
/// <typeparam name="TNode">The type of the node.</typeparam>
public class NodeDeltaType<TNode> : ObjectGraphType<DeltaDto<TNode>>, INodeDeltaType
    where TNode : Node
{
    /// <summary>Initializes a new instance of the <see cref="NodeDeltaType{TNode}"/> class.</summary>
    public NodeDeltaType()
    {
        var typeName = typeof(TNode).Name.Replace("`", string.Empty);
        Name = $"{typeName}Delta";
        Description = $"Represents changes for {typeName}.";

        var updatedAtProperty = ExpressionReflector.GetProperty<DeltaDto<TNode>>(d => d.UpdatedAt);
        Field<ObjectIdGraphType>(updatedAtProperty.Name)
            .Description(updatedAtProperty.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }));

        var deletedProperty = ExpressionReflector.GetProperty<DeltaDto<TNode>>(d => d.Deleted);
        Field<BooleanGraphType>(deletedProperty.Name)
            .Description(deletedProperty.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }));
    }

    void INodeDeltaType.AddNodeReference(Query query)
    {
        var type = query.GetOrCreateGraphType(typeof(TNode));

        var oldProperty = ExpressionReflector.GetProperty<DeltaDto<TNode>>(d => d.Old);
        AddField(new()
        {
            Name = oldProperty.Name,
            Description = oldProperty.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }),
            Type = type.GetType(),
            ResolvedType = type,
        });

        var newProperty = ExpressionReflector.GetProperty<DeltaDto<TNode>>(d => d.New);
        AddField(new()
        {
            Name = newProperty.Name,
            Description = newProperty.GetXmlDocsSummary(new() { ResolveExternalXmlDocs = false }),
            Type = type.GetType(),
            ResolvedType = type,
        });
    }
}
