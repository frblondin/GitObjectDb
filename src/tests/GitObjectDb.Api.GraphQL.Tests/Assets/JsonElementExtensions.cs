using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Z.Expressions;

namespace GitObjectDb.Api.GraphQL.Tests.Assets;

internal static class JsonElementExtensions
{
    private static readonly Regex _pathRegex = new($@"^(
                                                    (?<part>
                                                        \w+
                                                        ({CreateBalancedBracesRegex(@"\[", @"\]")})?
                                                    )
                                                    \.?)+$",
                                                   RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

    private static readonly Regex _partRegex = new($@"^(?<name>\w+)({CreateBalancedBracesRegex(@"\[", @"\]")})?$",
                                                   RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

    public static TValue? GetFromPath<TValue>(this JsonElement source, string path, JsonSerializerOptions? options = null)
    {
        var match = _pathRegex.Match(path);
        var result = source;
        foreach (var part in match.Groups["part"].Captures.OfType<Capture>())
        {
            result = GetNestedElement(result, part.Value);
        }
        return typeof(TValue) == typeof(JsonElement) ?
               (TValue)(object)result :
               result.Deserialize<TValue>(options);
    }

    private static JsonElement GetNestedElement(JsonElement result, string part)
    {
        var info = _partRegex.Match(part);
        try
        {
            result = GetNestedElement(result,
                                      info.Result("${name}"),
                                      info.Result("${index}"));
        }
        catch (Exception ex)
        {
            throw new NotSupportedException($"Could not get nested element '{part}'.", ex);
        }

        return result;
    }

    private static JsonElement GetNestedElement(JsonElement result, string name, string index)
    {
        result = result.GetProperty(name);
        if (!string.IsNullOrEmpty(index))
        {
            if (int.TryParse(index, out var position))
            {
                result = result[position];
            }
            else
            {
                var predicate = Eval.Compile<Func<JsonElement, bool>>(index, "item");
                result = result.EnumerateArray().Single(predicate);
            }
        }

        return result;
    }

    private static string CreateBalancedBracesRegex(string open, string close) => $@"
        {open}                  # First opening character
	    (?<index>
	        (?:
	        [^{open}{close}]    # Match all non-opening/closing chars
	        |
	        (?<open> {open} )   # Match opening, and capture into 'open'
	        |
	        (?<-open> {close} ) # Match closing, and delete the 'open' capture
	        )+
    	    (?(open)(?!))       # Fails if 'open' stack isn't empty!
        )
        {close}";
}
