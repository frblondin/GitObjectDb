using GitObjectDb.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;

namespace GitObjectDb;

/// <summary>A set of methods for instances of <see cref="IServiceCollection"/>.</summary>
public static class ServiceConfiguration
{
    /// <summary>Registers the serializer factory within the dependency injection framework.</summary>
    /// <param name="source">The configuration builder.</param>
    /// <param name="configure">Optional json serializer configuration.</param>
    /// <returns>The builder for chained calls.</returns>
    public static IServiceCollection AddGitObjectDbSystemTextJson(this IServiceCollection source,
                                                                  Action<JsonSerializerOptions>? configure = null)
    {
        source.AddSingleton<INodeSerializer, NodeSerializer>();

        if (configure is not null)
        {
            source.Configure(configure);
        }

        return source;
    }
}
