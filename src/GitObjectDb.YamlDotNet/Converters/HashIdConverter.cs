using GitDotNet;
using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace GitObjectDb.YamlDotNet.Converters;

internal class HashIdConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(HashId);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var value = parser.Consume<Scalar>().Value;
        return new HashId(value);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var id = (HashId)value!;
        var scalar = new Scalar(AnchorName.Empty,
                                TagName.Empty,
                                id.ToString()!,
                                ScalarStyle.Any,
                                true,
                                false);
        emitter.Emit(scalar);
    }
}
