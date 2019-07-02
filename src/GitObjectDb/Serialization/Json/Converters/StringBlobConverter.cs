using GitObjectDb.Attributes;
using GitObjectDb.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Serialization.Json.Converters
{
    /// <summary>
    /// Converts a <see cref="UniqueId" /> to and from a string (e.g. <c>"xK0hg6876bQ"</c>).
    /// </summary>
    public class StringBlobConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            return objectType == typeof(StringBlob);
        }

        /// <inheritdoc/>
        [ExcludeFromGuardForNull]
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (reader.TokenType == JsonToken.Null)
            {
                return new StringBlob(string.Empty);
            }
            if (reader.TokenType == JsonToken.String)
            {
                var modelReader = reader as JsonModelObjectReader ?? throw new JsonSerializationException($"Expected {nameof(JsonModelObjectReader)} object value.");
                var data = modelReader.RelativeFileDataResolver((string)reader.Value) ?? string.Empty;
                return new StringBlob(data);
            }
            throw new JsonSerializationException($"Unexpected token or value when parsing {nameof(UniqueId)}. Token: {reader.TokenType}, Value: {reader.Value}.");
        }

        /// <inheritdoc/>
        [ExcludeFromGuardForNull]
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            var modelWriter = writer as JsonModelObjectWriter ?? throw new JsonSerializationException($"Expected {nameof(JsonModelObjectWriter)} object value.");
            var info = new ModelNestedObjectInfo($"{writer.Path}{FileSystemStorage.BlobExtension}", new StringBuilder((value as StringBlob)?.Value ?? string.Empty));
            modelWriter.AdditionalObjects.Add(info);

            writer.WriteValue(info.FileName);
        }
    }
}
