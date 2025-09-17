using GitDotNet;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitObjectDb.SystemTextJson.Converters;

internal class HashIdConverter : JsonConverter<HashId>
{
    public override HashId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString() ?? throw new JsonException("Reader did not return any string value."));

    public override void Write(Utf8JsonWriter writer, HashId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
