using GraphQL.Types;
using GraphQLParser.AST;
using LibGit2Sharp;

namespace GitObjectDb.Api.GraphQL.Graph.Scalars;

internal class ObjectIdGraphType : ScalarGraphType
{
    public ObjectIdGraphType()
    {
        Description = "Represents a unique id in GitObjectDb.";
    }

    /// <inheritdoc />
    public override object? ParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLStringValue stringValue => new ObjectId((string)stringValue.Value),
        GraphQLNullValue _ => null,
        _ => ThrowLiteralConversionError(value, null),
    };

    /// <inheritdoc />
    public override object? ParseValue(object? value) => value switch
    {
        ObjectId _ => value,
        string s => new ObjectId(s),
        null => null,
        _ => ThrowValueConversionError(value),
    };

    /// <inheritdoc />
    public override object? Serialize(object? value) => value switch
    {
        ObjectId id => id.ToString(),
        null => null,
        _ => ThrowSerializationError(value),
    };
}
