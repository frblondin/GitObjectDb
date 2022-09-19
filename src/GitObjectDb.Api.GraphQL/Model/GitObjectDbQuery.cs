using Fasterflect;
using GitObjectDb.Api.Model;
using GitObjectDb.Model;
using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.Model;

public partial class GitObjectDbQuery : ObjectGraphType
{
    private readonly Dictionary<TypeDescription, INodeType> _typeToGraphType = new();

    public GitObjectDbQuery(IDataModel model)
    {
        Model = model;
        Name = "Query";
        DtoEmitter = new DtoTypeEmitter(Model);

        foreach (var description in DtoEmitter.TypeDescriptions)
        {
            AddCollectionField(this, description);
        }
    }

    public DtoTypeEmitter DtoEmitter { get; }

    public IDataModel Model { get; }

    internal INodeType GetOrCreateGraphType(TypeDescription description)
    {
        if (!_typeToGraphType.TryGetValue(description, out var result))
        {
            var schemaType = typeof(NodeType<,>).MakeGenericType(description.NodeType.Type, description.DtoType);
            var factory = Reflect.Constructor(schemaType);
            _typeToGraphType[description] = result = (INodeType)factory.Invoke();

            result.AddReferences(this);
            result.AddChildren(this);
        }
        return result;
    }
}
