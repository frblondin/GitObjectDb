using LibGit2Sharp;
using ProtoBuf;

namespace GitObjectDb.Api.ProtoBuf.Model.Surrogates;

[ProtoContract]
internal class ObjectIdSurrogate
{
    [ProtoMember(1)]
    public string? Sha { get; init; }

    public static implicit operator ObjectIdSurrogate?(ObjectId? value)
    {
        if (value is null)
        {
            return null;
        }
        return new ObjectIdSurrogate
        {
            Sha = value.Sha,
        };
    }

    public static implicit operator ObjectId?(ObjectIdSurrogate? value)
    {
        if (value?.Sha is null)
        {
            return null;
        }
        return new ObjectId(value.Sha);
    }
}
