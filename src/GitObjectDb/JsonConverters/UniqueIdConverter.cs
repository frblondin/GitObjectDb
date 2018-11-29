using GitObjectDb.Attributes;
using GitObjectDb.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.JsonConverters
{
    /// <summary>
    /// Converts a <see cref="UniqueId" /> to and from a string (e.g. <c>"xK0hg6876bQ"</c>).
    /// </summary>
    public class UniqueIdConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            return objectType == typeof(UniqueId);
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
                return null;
            }
            if (reader.TokenType == JsonToken.String)
            {
                try
                {
                    return new UniqueId((string)reader.Value);
                }
                catch (Exception ex)
                {
                    throw new JsonSerializationException($"Error parsing {nameof(UniqueId)} string: {reader.Value}", ex);
                }
            }
            throw new JsonSerializationException($"Unexpected token or value when parsing {nameof(UniqueId)}. Token: {reader.TokenType}, Value: {reader.Value}");
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

            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            if (value is UniqueId id)
            {
                writer.WriteValue(id.ToString());
                return;
            }
            throw new JsonSerializationException($"Expected {nameof(UniqueId)} object value");
        }
    }
}
