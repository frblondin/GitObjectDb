using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.Model;

public class NodeType<TNode, TNodeDTO> : ObjectGraphType<TNodeDTO>
{
    public NodeType(GitObjectDbQuery query)
    {
        Name = typeof(TNode).Name;

        AddProperties();
        AddChildren(query);

        AddField(new FieldType
        {
            Name = "History",
            Type = typeof(ListGraphType<CommitType>),
            Resolver = new FuncFieldResolver<object?, object?>(GitObjectDbQuery.QueryLog),
        });

        Interface<NodeInterface>();
    }

    private void AddProperties()
    {
        foreach (var property in typeof(TNodeDTO).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!SchemaTypes.BuiltInScalarMappings.ContainsKey(property.PropertyType))
            {
                continue;
            }
            var type = property.PropertyType.GetGraphTypeFromType(isNullable: true, TypeMappingMode.OutputType);
            Field(type, property.Name);
        }
    }

    private void AddChildren(GitObjectDbQuery query)
    {
        var description = query.Model.GetDescription(typeof(TNode));
        foreach (var childType in description.Children)
        {
            var dtoEmitterInfo = query.DtoEmitter.TypeDescriptions.First(d => d.NodeType == childType);
            GitObjectDbQuery.AddCollectionField(query, this, dtoEmitterInfo);
        }
    }
}
