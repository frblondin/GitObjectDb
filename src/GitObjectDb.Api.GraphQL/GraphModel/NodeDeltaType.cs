using GitObjectDb.Api.GraphQL.Model;
using GitObjectDb.Api.GraphQL.Tools;
using GraphQL.Types;
using Models.Organization;
using Namotion.Reflection;

namespace GitObjectDb.Api.GraphQL.GraphModel;

internal class NodeDeltaType<TNode> : ObjectGraphType<DeltaDto<TNode>>, INodeDeltaType
    where TNode : Node
{
    public NodeDeltaType()
    {
        var typeName = typeof(TNode).Name.Replace("`", string.Empty);
        Name = $"{typeName}Delta";
        Description = $"Represents changes for {typeName}.";

        var updatedAtProperty = ExpressionReflector.GetProperty<DeltaDto<TNode>>(d => d.UpdatedAt);
        Field<ObjectIdGraphType>(updatedAtProperty.Name)
            .Description(updatedAtProperty.GetXmlDocsSummary(false));

        var deletedProperty = ExpressionReflector.GetProperty<DeltaDto<TNode>>(d => d.Deleted);
        Field<BooleanGraphType>(deletedProperty.Name)
            .Description(deletedProperty.GetXmlDocsSummary(false));
    }

    void INodeDeltaType.AddNodeReference(GitObjectDbQuery query)
    {
        var type = query.GetOrCreateGraphType(typeof(TNode));

        var oldProperty = ExpressionReflector.GetProperty<DeltaDto<TNode>>(d => d.Old);
        AddField(new()
        {
            Name = oldProperty.Name,
            Description = oldProperty.GetXmlDocsSummary(false),
            Type = type.GetType(),
            ResolvedType = type,
        });

        var newProperty = ExpressionReflector.GetProperty<DeltaDto<TNode>>(d => d.New);
        AddField(new()
        {
            Name = newProperty.Name,
            Description = newProperty.GetXmlDocsSummary(false),
            Type = type.GetType(),
            ResolvedType = type,
        });
    }
}
