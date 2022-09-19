using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Api.GraphQL.Loaders;
using GitObjectDb.Api.Model;
using GitObjectDb.Injection;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL;

/// <summary>A set of methods for instances of <see cref="IServiceCollection"/>.</summary>
public static class ServiceConfiguration
{
    /// <summary>Adds access to GitObjectDb repositories.</summary>
    /// <param name="source">The source.</param>
    /// <param name="configure">The GraphQL schema additional configuration.</param>
    /// <returns>The source <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddGitObjectDbGraphQL(this IServiceCollection source, Action<IGitObjectDbGraphQLBuilder> configure)
    {
        // Avoid double-registrations
        if (source.IsGitObjectDbGraphQLRegistered())
        {
            throw new NotSupportedException("GitObjectDbGraphQL has already been registered.");
        }

        var emitter = source.FirstOrDefault(s => s.ServiceType == typeof(DtoTypeEmitter) &&
            s.Lifetime == ServiceLifetime.Singleton &&
            s.ImplementationInstance is not null)?.ImplementationInstance as DtoTypeEmitter ??
            throw new NotSupportedException($"GitObjectDbApi has not bee registered..");

        var schema = new GitObjectDbSchema(emitter);
        var configuration = new GitObjectDbGraphQLBuilder(schema);
        configure.Invoke(configuration);

        if (configuration.CacheEntryStrategy is null)
        {
            throw new NotSupportedException($"The {nameof(IGitObjectDbGraphQLBuilder.CacheEntryStrategy)} cannot be null.");
        }

        var query = (GitObjectDbQuery)schema.Query;

        source
            .AddSingleton<ISchema>(schema)
            .AddSingleton(configuration.CacheEntryStrategy)
            .AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>()
            .AddSingleton(typeof(NodeDataLoader<,>))
            .AddSingleton(typeof(NodeDeltaDataLoader<,>))
            .AddSingleton<DataLoaderDocumentListener>()
            .AddGraphQL(builder => builder
                .UseApolloTracing(true)
                .AddSystemTextJson()
                .AddGraphTypes(query.DtoEmitter.AssemblyBuilder));
        return source;
    }

    /// <summary>Gets whether GitObjectDbApi has already been registered.</summary>
    /// <param name="source">The source.</param>
    /// <returns><c>true</c> if the service has already been registered, <c>false</c> otherwise.</returns>
    private static bool IsGitObjectDbGraphQLRegistered(this IServiceCollection source) =>
        source.Any(sd => sd.ServiceType == typeof(ISchema));
}
