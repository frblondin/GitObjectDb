using GitObjectDb.Injection;
using GitObjectDb.YamlDotNet;
using System;
using YamlDotNet.Serialization;

namespace GitObjectDb;

/// <summary>A set of methods for instances of <see cref="IGitObjectDbBuilder"/>.</summary>
public static class GitObjectDbBuilderExtensions
{
    /// <summary>Registers the serializer factory within the dependency injection framework.</summary>
    /// <param name="source">The configuration builder.</param>
    /// <param name="namingConvention">The naming convention for property names.</param>
    /// <param name="configureSerializer">Optional yaml serializer configuration.</param>
    /// <param name="configureDeserializer">Optional yaml deserializer configuration.</param>
    /// <returns>The builder for chained calls.</returns>
    public static IGitObjectDbBuilder AddYamlDotNet(this IGitObjectDbBuilder source,
                                                    INamingConvention namingConvention,
                                                    Action<SerializerBuilder>? configureSerializer = null,
                                                    Action<DeserializerBuilder>? configureDeserializer = null)
    {
        source.AddSerializer(model => new NodeSerializer(model,
                                                         namingConvention,
                                                         configureSerializer,
                                                         configureDeserializer));

        return source;
    }
}
