using ProtoBuf;

namespace GitObjectDb.Api.ProtoBuf.Model;

/// <summary>Describes the delta request parameters.</summary>
/// <param name="Start">Start committish of the delta query.</param>
/// <param name="End">End committish of the delta query.</param>
[ProtoContract]
public record NodeDeltaQueryRequest([property: ProtoMember(1)] string? Start,
                                    [property: ProtoMember(2)] string? End)
{
    private NodeDeltaQueryRequest()
        : this(null, null)
    {
    }

    internal void Validate()
    {
        if (Start is null)
        {
            throw new ArgumentNullException(nameof(Start));
        }
        if (End is null)
        {
            throw new ArgumentNullException(nameof(End));
        }
    }
}
