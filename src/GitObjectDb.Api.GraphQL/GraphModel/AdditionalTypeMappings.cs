using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.GraphModel;
internal static class AdditionalTypeMappings
{
    internal static void Add(Schema schema)
    {
        schema.RegisterTypeMapping(typeof(NodeDto), typeof(NodeInterface));
        schema.RegisterTypeMapping<TimeZoneInfo, TimeZoneInfoGraphType>();

        ValueConverter.Register<string, TimeZoneInfo>(value => TimeZoneInfo.FromSerializedString(value));
    }

    internal static bool IsScalarType(Type type) =>
        type == typeof(TimeZoneInfo) ||
        SchemaTypes.BuiltInScalarMappings.ContainsKey(type);
}
