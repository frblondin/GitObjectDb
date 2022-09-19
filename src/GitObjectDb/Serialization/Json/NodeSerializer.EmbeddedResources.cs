using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GitObjectDb.Serialization.Json
{
    internal partial class NodeSerializer : INodeSerializer
    {
        private static string? ReadEmbeddedResource(in ReadOnlySequence<byte> jsonData)
        {
            var reader = new Utf8JsonReader(jsonData, new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Allow,
            });
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                {
                    var comment = reader.GetComment();
                    var unescaped = UnescapeComment(comment);
                    return RemoveSurroundingCarriageReturns(unescaped);
                }
            }
            return null;
        }

        private static void WriteEmbeddedResource(Node node, Utf8JsonWriter writer)
        {
            if (node.EmbeddedResource is not null)
            {
                var value = EscapeComment(node.EmbeddedResource);
                var surroundedByCarriageReturn = SurroundWithCarriageReturns(value);
                writer.WriteCommentValue(surroundedByCarriageReturn);
            }
        }

        private static string EscapeComment(string value) =>
            value.IndexOf(CommentStringToEscape) != -1 ?
            value.Replace(CommentStringToEscape, CommentStringToUnescape) :
            value;

        private static string UnescapeComment(string value) =>
            value.IndexOf(CommentStringToUnescape) != -1 ?
            value.Replace(CommentStringToUnescape, CommentStringToEscape) :
            value;

        private static unsafe string SurroundWithCarriageReturns(string value)
        {
            var result = new string('\n', value.Length + 2);
            fixed (char* source = value)
            {
                fixed (char* target = result)
                {
                    Unsafe.CopyBlock(target + 1, source, (uint)value.Length * sizeof(char));
                }
            }
            return result;
        }

        private static string? RemoveSurroundingCarriageReturns(string value) =>
            value.Length > 2 ?
            value.Substring(1, value.Length - 2) :
            null;
    }
}
