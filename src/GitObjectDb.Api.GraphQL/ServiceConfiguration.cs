using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Api.GraphQL.Loaders;
using GitObjectDb.Model;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.DI;
using GraphQL.Types;
using LibGit2Sharp;
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
                                                                 Action<GitObjectDbGraphQLOptions> configure)
    {
        // Avoid double-registrations
        if (source.IsGitObjectDbGraphQLRegistered())
        {
            throw new NotSupportedException("GitObjectDbGraphQL has already been registered.");
        }

        return source
            .AddSingleton<ISchema, GitObjectDbSchema>()
            .AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>()
            .AddSingleton(typeof(NodeDataLoader<>))
            .AddSingleton(typeof(NodeDeltaDataLoader<>))
            .AddSingleton<DataLoaderDocumentListener>()
            .Configure(configure);
    }

    /// <summary>Gets whether GitObjectDbApi has already been registered.</summary>
    /// <param name="source">The source.</param>
    /// <returns><c>true</c> if the service has already been registered, <c>false</c> otherwise.</returns>
    private static bool IsGitObjectDbGraphQLRegistered(this IServiceCollection source) =>
        source.Any(sd => sd.ServiceType == typeof(ISchema));
}
