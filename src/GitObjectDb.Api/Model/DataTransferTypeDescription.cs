using GitObjectDb.Model;
using System.Diagnostics.CodeAnalysis;

namespace GitObjectDb.Api.Model;

/// <summary>Describes the data transfer type information.</summary>
[ExcludeFromCodeCoverage]
public class DataTransferTypeDescription : IEquatable<DataTransferTypeDescription>
{
    /// <summary>Initializes a new instance of the <see cref="DataTransferTypeDescription"/> class.</summary>
    /// <param name="nodeType">The original node type description.</param>
    /// <param name="dtoType">The corresponding data transfer type.</param>
    public DataTransferTypeDescription(NodeTypeDescription nodeType, Type dtoType)
    {
        NodeType = nodeType;
        DtoType = dtoType;
    }

    /// <summary>Gets the original node type description.</summary>
    public NodeTypeDescription NodeType { get; }

    /// <summary>Gets the corresponding data transfer type.</summary>
    public Type DtoType { get; }

    /// <inheritdoc/>
    public bool Equals(DataTransferTypeDescription? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) ||
               (NodeType.Equals(other.NodeType) && DtoType == other.DtoType);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is null)
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

        return Equals((DataTransferTypeDescription)obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(NodeType, DtoType);
    }
}
