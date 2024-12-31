using NodaTime;
using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Models.Organization.Converters;

public class DateTimeZoneConverter(IDateTimeZoneProvider provider) : IYamlTypeConverter
{
    public bool Accepts(Type type) => typeof(DateTimeZone).IsAssignableFrom(type);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var value = parser.Consume<Scalar>().Value;
        return provider[value];
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var id = (DateTimeZone)value!;
        var scalar = new Scalar(AnchorName.Empty,
                                TagName.Empty,
                                id.Id,
                                ScalarStyle.Any,
                                true,
                                false);
        emitter.Emit(scalar);
    }
}
