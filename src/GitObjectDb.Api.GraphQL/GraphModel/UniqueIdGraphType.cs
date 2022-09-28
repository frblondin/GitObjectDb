using GitObjectDb;
using GraphQL.Types;
using GraphQLParser.AST;

namespace Models.Organization;

public class UniqueIdGraphType : ScalarGraphType
{
    public UniqueIdGraphType()
    {
        Description = "Represents a unique id in GitObjectDb.";
    }

    /// <inheritdoc />
    public override object? ParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLStringValue stringValue => new UniqueId((string)stringValue.Value),
        GraphQLNullValue _ => null,
        _ => ThrowLiteralConversionError(value, null),
    };

    /// <inheritdoc />
    public override object? ParseValue(object? value) => value switch
    {
        UniqueId _ => value,
        string s => new UniqueId(s),
        null => null,
        _ => ThrowValueConversionError(value),
    };

    /// <inheritdoc />
    public override object? Serialize(object? value) => value switch
    {
        UniqueId id => id.ToString(),
        null => null,
        _ => ThrowSerializationError(value),
    };
}
