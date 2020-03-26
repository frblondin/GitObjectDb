using Fasterflect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitObjectDb.Serialization.Json.Converters
{
    internal class NodeConverter<TNode> : JsonConverter<TNode>
        where TNode : Node
    {
        private readonly string _idName;
        private readonly IDictionary<string, (PropertyInfo Property, MemberGetter Getter, MemberSetter Setter)> _properties;
        private readonly ConstructorInvoker _activator = Reflect.Constructor(typeof(TNode), typeof(UniqueId));

        public NodeConverter(JsonSerializerOptions options)
            : base()
        {
            _idName = options.PropertyNamingPolicy?.ConvertName(nameof(Node.Id)) ?? nameof(Node.Id);
            _properties = typeof(TNode).GetTypeInfo()
                .GetProperties()
                .Where(p => p.CanRead && p.CanWrite && !Attribute.IsDefined(p, typeof(JsonIgnoreAttribute), true))
                .ToDictionary(DetermineProperytName, GetMemberAccessors, StringComparer.Ordinal);
            _activator = Reflect.Constructor(typeof(TNode), typeof(UniqueId));

            string DetermineProperytName(PropertyInfo property)
            {
                var attribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
                if (attribute != null)
                {
                    if (string.IsNullOrWhiteSpace(attribute.Name))
                    {
                        throw new InvalidOperationException($"The Json property name for '{property.DeclaringType}.{property.Name}' cannot be null.");
                    }
                    return attribute.Name;
                }
                else if (options.PropertyNamingPolicy != null)
                {
                    return options.PropertyNamingPolicy.ConvertName(property.Name) ??
                        throw new InvalidOperationException($"The Json property name for '{property.DeclaringType}.{property.Name}' cannot be null.");
                }
                return property.Name;
            }
            (PropertyInfo, MemberGetter, MemberSetter) GetMemberAccessors(PropertyInfo property)
            {
                return (property, Reflect.PropertyGetter(property), Reflect.PropertySetter(property));
            }
        }

        public override TNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var (id, setters) = ReadPropertyValues(ref reader, options);
            if (!id.HasValue)
            {
                throw new JsonException("Missing id in Json structure.");
            }
            var result = (TNode)_activator(id.Value);
            foreach (var info in setters)
            {
                info.Setter(result, info.Value);
            }
            return result;
        }

        private (UniqueId?, IEnumerable<(MemberSetter Setter, object Value)>) ReadPropertyValues(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            UniqueId? id = default;
            var setters = new List<(MemberSetter Setter, object Value)>();
            (PropertyInfo Property, MemberGetter Getter, MemberSetter Setter)? currentProperty = default;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        var name = reader.GetString();
                        if (name.Equals(nameof(Node.Id), StringComparison.OrdinalIgnoreCase))
                        {
                            ReadNextToken(ref reader, JsonTokenType.String);
                            id = new UniqueId(reader.GetString());
                        }
                        else
                        {
                            currentProperty = _properties.TryGetValue(name, out var info) ?
                                ((PropertyInfo Property, MemberGetter Getter, MemberSetter Setter)?)info :
                                null;
                        }
                        break;
                    case JsonTokenType.Comment:
                        break;
                    case JsonTokenType.EndObject:
                        return (id, setters);
                    default:
                        if (currentProperty.HasValue)
                        {
                            var value = JsonSerializer.Deserialize(ref reader, currentProperty.Value.Property.PropertyType, options);
                            setters.Add((currentProperty.Value.Setter, value));
                        }
                        else
                        {
                            ReadIgnoredValue(ref reader);
                        }
                        break;
                }
            }
            return default;
        }

        private static void ReadIgnoredValue(ref Utf8JsonReader reader)
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

        private static void ReadNextToken(ref Utf8JsonReader reader, JsonTokenType expectedToken, string? expectedString = null)
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

        public override void Write(Utf8JsonWriter writer, TNode value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(_idName, value.Id.ToString());
            foreach (var kvp in _properties)
            {
                var propertyValue = kvp.Value.Getter(value);
                if (propertyValue != null || !options.IgnoreNullValues)
                {
                    writer.WritePropertyName(kvp.Key);
                    JsonSerializer.Serialize(writer, propertyValue, kvp.Value.Property.PropertyType, options);
                }
            }
            writer.WriteEndObject();
        }
    }
}
