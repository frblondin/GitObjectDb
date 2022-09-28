using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Api.GraphQL.Loaders;
using GitObjectDb.Model;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.DI;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using ServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;

namespace GitObjectDb.Api.GraphQL;

/// <summary>A set of methods for instances of <see cref="IServiceCollection"/>.</summary>
public static class ServiceConfiguration
{
    /// <summary>Adds access to GitObjectDb repositories.</summary>
    /// <param name="source">The source.</param>
    /// <param name="configure">The GraphQL schema additional configuration.</param>
    /// <returns>The source <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddGitObjectDbGraphQLSchema(this IServiceCollection source,
                                                                 Action<IGitObjectDbGraphQLBuilder> configure)
    {
        // Avoid double-registrations
        if (source.IsGitObjectDbGraphQLRegistered())
        {
            throw new NotSupportedException("GitObjectDbGraphQL has already been registered.");
        }

        var model = source.FirstOrDefault(s => s.ServiceType == typeof(IDataModel) &&
            s.Lifetime == ServiceLifetime.Singleton &&
            s.ImplementationInstance is not null)?.ImplementationInstance as IDataModel ??
            throw new NotSupportedException($"IDataModel has not bee registered.");

        var schema = new GitObjectDbSchema(model);
        var configuration = InitializeSchema(configure, schema);

        return source
            .AddSingleton<ISchema>(schema)
            .AddSingleton(configuration.CacheEntryStrategy!)
            .AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>()
            .AddSingleton(typeof(NodeDataLoader<>))
            .AddSingleton(typeof(NodeDeltaDataLoader<>))
            .AddSingleton<DataLoaderDocumentListener>();
    }

    private static GitObjectDbGraphQLBuilder InitializeSchema(Action<IGitObjectDbGraphQLBuilder> configure, GitObjectDbSchema schema)
    {
        var configuration = new GitObjectDbGraphQLBuilder(schema);
        AdditionalTypeMappings.Add(schema);
        configure.Invoke(configuration);
        schema.Query = new GitObjectDbQuery(schema);
        schema.Mutation = new GitObjectDbMutation(schema);
        if (configuration.CacheEntryStrategy is null)
        {
            throw new NotSupportedException($"The {nameof(IGitObjectDbGraphQLBuilder.CacheEntryStrategy)} cannot be null.");
        }
        return configuration;
    }

    /// <summary>Gets whether GitObjectDbApi has already been registered.</summary>
    /// <param name="source">The source.</param>
    /// <returns><c>true</c> if the service has already been registered, <c>false</c> otherwise.</returns>
    private static bool IsGitObjectDbGraphQLRegistered(this IServiceCollection source) =>
        source.Any(sd => sd.ServiceType == typeof(ISchema));
}
