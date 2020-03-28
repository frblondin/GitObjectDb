using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitObjectDb.Serialization.Json.Converters
{
    internal class NonScalarConverter : JsonConverter<NonScalar>
    {
        private static readonly ConcurrentDictionary<string, Type> _typeCache = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        public override NonScalar Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var type = ReadType(ref reader);

            Utf8JsonReaderHelper.ReadNextToken(ref reader, JsonTokenType.PropertyName, nameof(NonScalar.Node));

            Utf8JsonReaderHelper.ReadNextToken(ref reader, JsonTokenType.StartObject);
            var result = (Node)JsonSerializer.Deserialize(ref reader, type, options);
            Utf8JsonReaderHelper.ReadNextToken(ref reader, JsonTokenType.EndObject);

            return new NonScalar(result);
        }

        private Type ReadType(ref Utf8JsonReader reader)
        {
            Utf8JsonReaderHelper.ReadNextToken(ref reader, JsonTokenType.PropertyName, nameof(NonScalar.Type));
            Utf8JsonReaderHelper.ReadNextToken(ref reader, JsonTokenType.String);

            var typeName = reader.GetString();
            var type = _typeCache.GetOrAdd(typeName, BindToType);
            return type;
        }

        public override void Write(Utf8JsonWriter writer, NonScalar value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(NonScalar.Type), BindToName(value.Node.GetType()));
            writer.WritePropertyName(nameof(NonScalar.Node));
            JsonSerializer.Serialize(writer, value.Node, value.Node.GetType(), options);

            writer.WriteEndObject();
        }

        internal static string BindToName(Type type) => $"{type.FullName}, {type.Assembly.FullName}";

        private static Type BindToType(string name)
        {
            var index = GetAssemblyDelimiterIndex(name);

            var assemblyName = name.Substring(index + 1).Trim();
            var assembly = Assembly.Load(assemblyName);

            var typeName = name.Substring(0, index).Trim();
            var type = assembly.GetType(typeName);

            return type;
        }

        private static int GetAssemblyDelimiterIndex(string fullyQualifiedTypeName)
        {
            int num = 0;
            for (int i = 0; i < fullyQualifiedTypeName.Length; i++)
            {
                char c = fullyQualifiedTypeName[i];
                if (c != ',')
                {
                    if (c != '[')
                    {
                        if (c == ']')
                        {
                            num--;
                        }
                    }
                    else
                    {
                        num++;
                    }
                }
                else if (num == 0)
                {
                    return i;
                }
            }
            throw new JsonException();
        }
    }
}
