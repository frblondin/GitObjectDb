using System.Text.Json;

namespace GitObjectDb.Serialization.Json.Converters;

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
}
