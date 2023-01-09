using ProtoBuf;

namespace GitObjectDb.Api.ProtoBuf.Model;

/// <summary>Describes the node request parameters.</summary>
/// <param name="Committish">The committish.</param>
/// <param name="ParentPath">The parent node path.</param>
/// <param name="IsRecursive"><c>true</c> to query all nodes recursively, <c>false</c> otherwise.</param>
[ProtoContract]
public record NodeQueryRequest([property: ProtoMember(1)] string? Committish,
                               [property: ProtoMember(2)] DataPath? ParentPath = null,
                               [property: ProtoMember(3)] bool IsRecursive = false)
{
    private NodeQueryRequest()
        : this(null, null, default)
    {
    }

    internal void Validate()
    {
        if (Committish is null)
        {
            throw new ArgumentNullException(nameof(Committish));
        }
    }
}
