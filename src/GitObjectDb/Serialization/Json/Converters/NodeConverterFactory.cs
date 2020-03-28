using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitObjectDb.Serialization.Json.Converters
{
    internal class NodeConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeof(Node).IsAssignableFrom(typeToConvert);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var inner = GetInternalDefaultConverter(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(
                typeof(NodeConverter<>).MakeGenericType(new[] { typeToConvert }),
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new object[] { inner },
                null);
        }

        private static JsonConverter GetInternalDefaultConverter(Type typeToConvert)
        {
            return new JsonSerializerOptions().GetConverter(typeToConvert);
        }
    }
}