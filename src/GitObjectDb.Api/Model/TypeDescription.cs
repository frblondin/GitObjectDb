using GitObjectDb.Model;
using System.Diagnostics.CodeAnalysis;

namespace GitObjectDb.Api.Model;

[ExcludeFromCodeCoverage]
public class TypeDescription : IEquatable<TypeDescription>
{
    public TypeDescription(NodeTypeDescription nodeType, Type dtoType)
    {
        NodeType = nodeType;
        DtoType = dtoType;
    }

    public NodeTypeDescription NodeType { get; }

    public Type DtoType { get; }

    public bool Equals(TypeDescription? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        return ReferenceEquals(this, other) ||
               (NodeType.Equals(other.NodeType) && DtoType == other.DtoType);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((TypeDescription)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(NodeType, DtoType);
    }
}
