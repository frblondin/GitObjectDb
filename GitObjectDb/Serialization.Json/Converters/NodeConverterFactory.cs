using System;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitObjectDb.Serialization.Json.Converters
{
    internal class NodeConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeof(Node).IsAssignableFrom(typeToConvert);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(NodeConverter<>).MakeGenericType(new[] { typeToConvert }),
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new object[] { options },
                null);
    }
}
