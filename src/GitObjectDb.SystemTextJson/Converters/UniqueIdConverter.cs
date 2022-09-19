using GitObjectDb.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitObjectDb.SystemTextJson.Converters;

internal class UniqueIdConverter : JsonConverter<UniqueId>
{
    public override UniqueId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString() ?? throw new JsonException("Reader did not return any string value."));

    public override void Write(Utf8JsonWriter writer, UniqueId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
