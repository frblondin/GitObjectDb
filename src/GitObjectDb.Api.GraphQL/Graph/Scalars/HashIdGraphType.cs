using GitDotNet;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GitObjectDb.Api.GraphQL.Graph.Scalars;

internal class HashIdGraphType : ScalarGraphType
{
    public HashIdGraphType()
    {
        Description = "Represents a unique id in GitObjectDb.";
    }

    /// <inheritdoc />
    public override object? ParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLStringValue stringValue => new HashId((string)stringValue.Value),
        GraphQLNullValue _ => null,
        _ => ThrowLiteralConversionError(value, null),
    };

    /// <inheritdoc />
    public override object? ParseValue(object? value) => value switch
    {
        HashId _ => value,
        string s => new HashId(s),
        null => null,
        _ => ThrowValueConversionError(value),
    };

    /// <inheritdoc />
    public override object? Serialize(object? value) => value switch
    {
        HashId id => id.ToString(),
        null => null,
        _ => ThrowSerializationError(value),
    };
}
