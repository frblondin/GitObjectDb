using GitObjectDb.Api.GraphQL.Tools;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using LibGit2Sharp;

namespace GitObjectDb.Api.GraphQL.Graph.Scalars;
internal static class ScalarTypes
{
    internal static void Add(ISchema schema)
    {
        schema.RegisterTypeMapping<ObjectId, ObjectIdGraphType>();
        schema.RegisterTypeMapping<UniqueId, UniqueIdGraphType>();
        schema.RegisterTypeMapping<DataPath, DataPathGraphType>();
    }

    /// <summary>
    /// Gets whether a type is valid to be added to a schema with supported type mapper.
    /// </summary>
    /// <param name="type">The type to be analyzed.</param>
    /// <param name="schema">The schema with defined type mappers.</param>
    /// <returns><c>true</c> if the type is valid, <c>false</c> otherwise.</returns>
    internal static bool IsValidScalarForGraph(this Type type, ISchema? schema = null)
    {
        var primaryType = type.GetGraphPrimaryType();
        Predicate<Type> predicate = schema is null ?
            static type => SchemaTypes.BuiltInScalarMappings.ContainsKey(type) ||
                           type.IsGitObjectDbScalarType() :
            type => schema.TypeMappings.Any(kvp => kvp.clrType == primaryType) ||
                    schema.BuiltInTypeMappings.Any(kvp => kvp.clrType == primaryType);
        return primaryType.IsEnum || predicate(type);
    }

    private static bool IsGitObjectDbScalarType(this Type type) =>
        type == typeof(ObjectId) ||
        type == typeof(UniqueId) ||
        type == typeof(DataPath);

    /// <summary>
    /// Gets the primary type, that is generic argument of an enumerable, underlying type for a nullable...
    /// </summary>
    /// <param name="type">The type to be analyzed.</param>
    /// <returns>The primary type.</returns>
    private static Type GetGraphPrimaryType(this Type type)
    {
        while (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDataLoaderResult<>))
        {
            type = type.GetGenericArguments()[0];
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return type.GetGenericArguments()[0].GetGraphPrimaryType();
        }
        if (type.IsArray)
        {
            return type.GetElementType()!.GetGraphPrimaryType();
        }
        if (type != typeof(string) && type.IsEnumerable(static _ => true, out var argumentType))
        {
            return argumentType!;
        }
        return type;
    }
}
