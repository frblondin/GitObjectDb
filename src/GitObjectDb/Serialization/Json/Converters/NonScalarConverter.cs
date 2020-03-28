using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitObjectDb.Serialization.Json.Converters
{
    internal class NonScalarConverter : JsonConverter<NonScalar>
    {
        private readonly INodeSerializer _nodeSerializer;

        public NonScalarConverter(INodeSerializer nodeSerializer)
        {
            _nodeSerializer = nodeSerializer ?? throw new ArgumentNullException(nameof(nodeSerializer));
        }

        public override NonScalar Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var type = ReadType(ref reader);

            Utf8JsonReaderHelper.ReadNextToken(ref reader, JsonTokenType.PropertyName, nameof(NonScalar.Node));

            Utf8JsonReaderHelper.ReadNextToken(ref reader, JsonTokenType.StartObject);
            var result = (Node)(JsonSerializer.Deserialize(ref reader, type, options) ??
                throw new JsonException("Value could not be deserialized."));
            Utf8JsonReaderHelper.ReadNextToken(ref reader, JsonTokenType.EndObject);

            return new NonScalar(result);
        }

        private Type ReadType(ref Utf8JsonReader reader)
        {
            Utf8JsonReaderHelper.ReadNextToken(ref reader, JsonTokenType.PropertyName, nameof(NonScalar.Type));
            Utf8JsonReaderHelper.ReadNextToken(ref reader, JsonTokenType.String);

            var typeName = reader.GetString() ??
                throw new JsonException("Reader did not return any string value.");
            return _nodeSerializer.BindToType(typeName);
        }

        public override void Write(Utf8JsonWriter writer, NonScalar value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(NonScalar.Type), _nodeSerializer.BindToName(value.Node.GetType()));
            writer.WritePropertyName(nameof(NonScalar.Node));
            JsonSerializer.Serialize(writer, value.Node, value.Node.GetType(), options);

            writer.WriteEndObject();
        }
    }
}
