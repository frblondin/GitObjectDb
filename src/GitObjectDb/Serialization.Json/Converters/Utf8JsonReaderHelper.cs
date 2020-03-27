using System.Text.Json;

namespace GitObjectDb.Serialization.Json.Converters
{
    internal static class Utf8JsonReaderHelper
    {
        internal static void ReadNextToken(ref Utf8JsonReader reader, JsonTokenType expectedToken, string? expectedString = null)
        {
            if (!reader.Read() || reader.TokenType != expectedToken)
            {
                throw new JsonException();
            }
            if (expectedString != null && reader.GetString() != expectedString)
            {
                throw new JsonException();
            }
        }

        internal static void ReadIgnoredValue(ref Utf8JsonReader reader)
        {
            var token = reader.TokenType;

            // Do a balanced read
            if (token == JsonTokenType.StartObject || token == JsonTokenType.StartArray)
            {
                var decrement = token == JsonTokenType.StartObject ? JsonTokenType.EndObject : JsonTokenType.EndArray;
                var level = 1;
                while (reader.Read())
                {
                    if (reader.TokenType == token)
                    {
                        level++;
                    }
                    if (reader.TokenType == decrement)
                    {
                        level--;
                        if (level == 0)
                        {
                            break;
                        }
                    }
                }
                if (level > 0)
                {
                    throw new JsonException();
                }
            }
        }
    }
}
