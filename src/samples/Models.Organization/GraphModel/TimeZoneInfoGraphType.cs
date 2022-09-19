using GraphQL.Types;
using GraphQLParser.AST;
using System;

namespace Models.Organization;

public class TimeZoneInfoGraphType : ScalarGraphType
{
    public TimeZoneInfoGraphType()
    {
        Description = "Represents any time zone in the world.";
    }

    /// <inheritdoc />
    public override object? ParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLStringValue stringValue => TimeZoneInfo.FromSerializedString((string)stringValue.Value),
        GraphQLNullValue _ => null,
        _ => ThrowLiteralConversionError(value, null),
    };

    /// <inheritdoc />
    public override object? ParseValue(object? value) => value switch
    {
        TimeZoneInfo _ => value,
        string s => TimeZoneInfo.FromSerializedString(s),
        null => null,
        _ => ThrowValueConversionError(value),
    };

    /// <inheritdoc />
    public override object? Serialize(object? value) => value switch
    {
        TimeZoneInfo timeZone => timeZone.ToSerializedString(),
        null => null,
        _ => ThrowSerializationError(value),
    };
}
