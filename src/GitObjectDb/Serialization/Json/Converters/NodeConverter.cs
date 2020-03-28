using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitObjectDb.Serialization.Json.Converters
{
    internal class NodeConverter<TNode> : JsonConverter<TNode>
        where TNode : Node
    {
        private readonly JsonConverter<TNode> _inner;

        public NodeConverter(JsonConverter<TNode> inner)
        {
            _inner = inner;
        }

        public override TNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return _inner.Read(ref reader, typeToConvert, options);
        }

        public override void Write(Utf8JsonWriter writer, TNode value, JsonSerializerOptions options)
        {
            _inner.Write(writer, value, options);
        }
    }
}
