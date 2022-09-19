using GitObjectDb.Injection;
using GitObjectDb.SystemTextJson;
using System;
using System.Text.Json;

namespace GitObjectDb;

/// <summary>A set of methods for instances of <see cref="IGitObjectDbBuilder"/>.</summary>
public static class GitObjectDbBuilderExtensions
{
    /// <summary>Registers the serializer factory within the dependency injection framework.</summary>
    /// <param name="source">The configuration builder.</param>
    /// <param name="configure">Optional json serializer configuration.</param>
    /// <returns>The builder for chained calls.</returns>
    public static IGitObjectDbBuilder AddSystemTextJson(this IGitObjectDbBuilder source,
                                                        Action<JsonSerializerOptions>? configure = null)
    {
        source.AddSerializer(model => new NodeSerializer(model, configure));

        return source;
    }
}
