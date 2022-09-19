using AutoMapper;
using GitObjectDb.Api.Model;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL.GraphModel;

internal class NodeDeltaType<TNode, TNodeDto> : ObjectGraphType<DeltaDto<TNodeDto>>, INodeDeltaType
{
    public NodeDeltaType()
    {
        Name = typeof(TNode).Name.Replace("`", string.Empty) + "Delta";

        Field<StringGraphType>(nameof(DeltaDto<TNodeDto>.UpdatedAt));
        Field<BooleanGraphType>(nameof(DeltaDto<TNodeDto>.Deleted));
    }

    void INodeDeltaType.AddNodeReference(GitObjectDbQuery query)
    {
        var type = query.GetOrCreateGraphType(typeof(TNodeDto), out var _);

        AddField(new()
        {
            Name = nameof(DeltaDto<TNodeDto>.Old),
            Description = "Gets the old node state.",
            Type = type.GetType(),
            ResolvedType = type,
        });
        AddField(new()
        {
            Name = nameof(DeltaDto<TNodeDto>.New),
            Description = "Gets the new node state.",
            Type = type.GetType(),
            ResolvedType = type,
        });
    }
}
