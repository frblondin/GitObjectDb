using GitObjectDb.Model;

namespace GitObjectDb.Api.Model;

public class TypeDescription
{
    public TypeDescription(NodeTypeDescription nodeType, Type dtoType)
    {
        NodeType = nodeType;
        DtoType = dtoType;
    }

    public NodeTypeDescription NodeType { get; }

    public Type DtoType { get; }
}
