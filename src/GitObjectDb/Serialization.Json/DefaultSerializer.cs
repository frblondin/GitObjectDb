using GitObjectDb.Serialization.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GitObjectDb.Serialization.Json
{
    internal class DefaultSerializer : INodeSerializer
    {
        private readonly JsonWriterOptions _writerOptions = new JsonWriterOptions
        {
            Indented = true,
        };

        private readonly JsonSerializerOptions _serializerOptions;

        public DefaultSerializer()
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

        public Stream Serialize(Node node)
        {
            var result = new MemoryStream();
            using var writer = new Utf8JsonWriter(result, _writerOptions);
            JsonSerializer.Serialize(writer, new NonScalar(node), _serializerOptions);
            result.Seek(0L, SeekOrigin.Begin);
            return result;
        }

        public NonScalar Deserialize(Stream stream, DataPath path)
        {
            using var reader = new Utf8JsonStreamReader(stream, 1024);
            reader.Read();

            var result = reader.Deserialize<NonScalar>(_serializerOptions);
            result.Node.Path = path;

            return result;
        }
    }
}
