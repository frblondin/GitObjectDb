using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Models.Organization.Converters;

public class TimeZoneInfoConverter : JsonConverter<TimeZoneInfo>, IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(TimeZoneInfo);

    public override TimeZoneInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        TimeZoneInfo.FromSerializedString(reader.GetString() ?? throw new JsonException("Reader did not return any string value."));

    public override void Write(Utf8JsonWriter writer, TimeZoneInfo value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToSerializedString());

    public object? ReadYaml(IParser parser, Type type)
    {
        var value = parser.Consume<Scalar>().Value;
        return TimeZoneInfo.FromSerializedString(value);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        var id = (TimeZoneInfo)value!;
        var scalar = new Scalar(AnchorName.Empty,
                                TagName.Empty,
                                id.ToSerializedString()!,
                                ScalarStyle.Any,
                                true,
                                false);
        emitter.Emit(scalar);
    }
}
