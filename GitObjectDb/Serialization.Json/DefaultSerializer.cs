using GitObjectDb.Serialization.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GitObjectDb.Serialization.Json
{
    internal static class DefaultSerializer
    {
        private static readonly JsonWriterOptions _writerOptions = new JsonWriterOptions
        {
            Indented = true,
        };

        private static readonly JsonSerializerOptions _serializerOptions;

        static DefaultSerializer()
        {
            _serializerOptions = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };
            _serializerOptions.Converters.Add(new NodeConverterFactory());
        }

        internal static Stream Serialize(NonScalar value)
        {
            var result = new MemoryStream();
            using var writer = new Utf8JsonWriter(result, _writerOptions);
            JsonSerializer.Serialize(writer, value, value.GetType(), _serializerOptions);
            result.Seek(0L, SeekOrigin.Begin);
            return result;
        }

        internal static NonScalar Deserialize(Stream stream, Path path)
        {
            using var reader = new Utf8JsonStreamReader(stream, 1024);
            reader.Read();

            var result = reader.Deserialize<NonScalar>(_serializerOptions);
            result.Node.Path = path;

            return result;
        }
    }
}
