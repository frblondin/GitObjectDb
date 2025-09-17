using GitDotNet;
using ProtoBuf;

namespace GitObjectDb.Api.ProtoBuf.Model.Surrogates;

[ProtoContract]
internal class HashIdSurrogate
{
    [ProtoMember(1)]
    public string? Sha { get; init; }

    public static implicit operator HashIdSurrogate?(HashId? value)
    {
        if (value is null)
        {
            return null;
        }
        return new HashIdSurrogate
        {
            Sha = value.ToString(),
        };
    }

    public static implicit operator HashId?(HashIdSurrogate? value)
    {
        if (value?.Sha is null)
        {
            return null;
        }
        return new HashId(value.Sha);
    }
}
