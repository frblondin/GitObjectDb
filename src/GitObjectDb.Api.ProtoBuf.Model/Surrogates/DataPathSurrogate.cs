using ProtoBuf;

namespace GitObjectDb.Api.ProtoBuf.Model.Surrogates;

[ProtoContract]
internal class DataPathSurrogate
{
    [ProtoMember(1)]
    public string? FilePath { get; init; }

    public static implicit operator DataPathSurrogate?(DataPath? value)
    {
        if (value is null)
        {
            return null;
        }
        return new DataPathSurrogate
        {
            FilePath = value.FilePath,
        };
    }

    public static implicit operator DataPath?(DataPathSurrogate? value)
    {
        if (value?.FilePath is null)
        {
            return null;
        }
        return DataPath.Parse(value.FilePath);
    }
}
