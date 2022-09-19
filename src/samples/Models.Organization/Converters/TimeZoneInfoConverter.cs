using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Models.Organization.Converters;

public class TimeZoneInfoConverter : JsonConverter<TimeZoneInfo>
{
    public override TimeZoneInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        TimeZoneInfo.FromSerializedString(reader.GetString() ?? throw new JsonException("Reader did not return any string value."));

    public override void Write(Utf8JsonWriter writer, TimeZoneInfo value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToSerializedString());
}
