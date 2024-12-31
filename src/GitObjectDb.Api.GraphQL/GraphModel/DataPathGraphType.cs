using GraphQL.Types;
using GraphQLParser.AST;

namespace GitObjectDb.Api.GraphQL.GraphModel;

public class DataPathGraphType : ScalarGraphType
{
    public DataPathGraphType()
    {
        Description = "Represents a path in GitObjectDb.";
    }

    /// <inheritdoc />
    public override object? ParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLStringValue stringValue => DataPath.Parse((string)stringValue.Value),
        GraphQLNullValue _ => null,
        _ => ThrowLiteralConversionError(value, null),
    };

    /// <inheritdoc />
    public override object? ParseValue(object? value) => value switch
    {
        DataPath _ => value,
        string s => DataPath.Parse(s),
        null => null,
        _ => ThrowValueConversionError(value),
    };

    /// <inheritdoc />
    public override object? Serialize(object? value) => value switch
    {
        DataPath path => path.ToString(),
        null => null,
        _ => ThrowSerializationError(value),
    };
}
