using GitObjectDb.Model;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Injection;

#pragma warning disable SA1402 // File may only contain a single type
/// <summary>An interface for configuring GitObjectDb services.</summary>
public interface IGitObjectDbBuilder
{
    /// <summary>Gets the collection of services.</summary>
    IServiceCollection Services { get; }
}

internal class GitObjectDbBuilder : IGitObjectDbBuilder
{
    public GitObjectDbBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }
}

/// <summary>A set of methods for instances of <see cref="IGitObjectDbBuilder"/>.</summary>
public static class GitObjectDbBuilderExtensions
{
    /// <summary>Registers the serializer factory within the dependency injection framework.</summary>
    /// <param name="source">The configuration builder.</param>
    /// <typeparam name="TNodeSerializer">The node serializer type.</typeparam>
    /// <returns>The builder for chained calls.</returns>
    public static IGitObjectDbBuilder AddSerializer<TNodeSerializer>(this IGitObjectDbBuilder source)
        where TNodeSerializer : class, INodeSerializer
    {
        source.Services.AddSingleton<TNodeSerializer>();

        return source;
    }

    /// <summary>Registers the serializer factory within the dependency injection framework.</summary>
    /// <param name="source">The configuration builder.</param>
    /// <param name="factory">The node serializer factory.</param>
    /// <returns>The builder for chained calls.</returns>
    public static IGitObjectDbBuilder AddSerializer(this IGitObjectDbBuilder source, INodeSerializer.Factory factory)
    {
        source.Services.AddSingleton(factory);

        return source;
    }
}