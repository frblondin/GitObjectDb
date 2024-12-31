using GraphQL.Types;
using GraphQLParser.AST;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Models.Organization;

public class DateTimeZoneGraphType : EnumerationGraphType
{
    private static Regex _invalidCharactersRegex = new("[^_a-zA-Z0-9]");
    private Dictionary<string, DateTimeZone> _timeZonesEnumNames = new();

    public DateTimeZoneGraphType()
    {
        Description = "Represents a time zone - a mapping between UTC and local time. " +
            "A time zone maps UTC instants to local times or, equivalently, to the offset " +
            "from UTC at any particular instant.\r\n" +
            "The mapping is unambiguous in the \"UTC to local\" direction, but the reverse " +
            "is not true: when the offset changes, usually due to a Daylight Saving transition, " +
            "the change either creates a gap (a period of local time which never occurs in the time zone) " +
            "or an ambiguity (a period of local time which occurs twice in the time zone). " +
            "Mapping back from local time to an instant requires consideration of how these problematic " +
            "times will be handled.";
        foreach (var timeZoneId in Organization.TimeZoneProvider.Ids)
        {
            var timeZone = Organization.TimeZoneProvider[timeZoneId];
            var modifiedName = SanitizeTimeZoneId(timeZoneId);
            _timeZonesEnumNames[modifiedName] = timeZone;
            Add(modifiedName, timeZone, timeZone.GetUtcOffset(Instant.FromDateTimeOffset(DateTimeOffset.Now)).ToString());
        }
    }

    private static string SanitizeTimeZoneId(string timeZoneId) =>
        _invalidCharactersRegex.Replace(timeZoneId.Replace("+", " plus ").Replace("-", " minus "), "_");

    /// <inheritdoc />
    public override object? ParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLEnumValue enumValue => _timeZonesEnumNames[enumValue.Name.StringValue],
        GraphQLNullValue _ => null,
        _ => ThrowLiteralConversionError(value, null),
    };

    /// <inheritdoc />
    public override object? ParseValue(object? value) => value switch
    {
        DateTimeZone _ => value,
        string s => _timeZonesEnumNames[s],
        null => null,
        _ => ThrowValueConversionError(value),
    };

    /// <inheritdoc />
    public override object? Serialize(object? value) => value switch
    {
        DateTimeZone timeZone => timeZone.Id,
        null => null,
        _ => ThrowSerializationError(value),
    };
}
