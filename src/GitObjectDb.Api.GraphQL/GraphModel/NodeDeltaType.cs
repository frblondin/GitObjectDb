using GitObjectDb.Api.GraphQL.Tools;
using GitObjectDb.Api.Model;
using GraphQL.Types;
using Namotion.Reflection;

namespace GitObjectDb.Api.GraphQL.GraphModel;

internal class NodeDeltaType<TNode, TNodeDto> : ObjectGraphType<DeltaDto<TNodeDto>>, INodeDeltaType
{
    public NodeDeltaType()
    {
        var typeName = typeof(TNode).Name.Replace("`", string.Empty);
        Name = $"{typeName}Delta";
        Description = $"Represents changes for {typeName}.";

        var updatedAtProperty = ExpressionReflector.GetProperty<DeltaDto<TNodeDto>>(d => d.UpdatedAt);
        Field<StringGraphType>(updatedAtProperty.Name, updatedAtProperty.GetXmlDocsSummary(false));

        var deletedProperty = ExpressionReflector.GetProperty<DeltaDto<TNodeDto>>(d => d.Deleted);
        Field<BooleanGraphType>(deletedProperty.Name, deletedProperty.GetXmlDocsSummary(false));
    }

    void INodeDeltaType.AddNodeReference(GitObjectDbQuery query)
    {
        var type = query.GetOrCreateGraphType(typeof(TNodeDto), out var _);

        var oldProperty = ExpressionReflector.GetProperty<DeltaDto<TNodeDto>>(d => d.Old);
        AddField(new()
        {
            Name = oldProperty.Name,
            Description = oldProperty.GetXmlDocsSummary(false),
            Type = type.GetType(),
            ResolvedType = type,
        });

        var newProperty = ExpressionReflector.GetProperty<DeltaDto<TNodeDto>>(d => d.New);
        AddField(new()
        {
            Name = newProperty.Name,
            Description = newProperty.GetXmlDocsSummary(false),
            Type = type.GetType(),
            ResolvedType = type,
        });
    }
}
