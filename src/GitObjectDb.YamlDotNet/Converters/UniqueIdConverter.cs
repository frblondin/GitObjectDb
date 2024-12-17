using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace GitObjectDb.YamlDotNet.Converters;

internal class UniqueIdConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(UniqueId);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var value = parser.Consume<Scalar>().Value;
        return new UniqueId(value);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var id = (UniqueId)value!;
        var scalar = new Scalar(AnchorName.Empty,
                                TagName.Empty,
                                id.ToString()!,
                                ScalarStyle.Any,
                                true,
                                false);
        emitter.Emit(scalar);
    }
}
