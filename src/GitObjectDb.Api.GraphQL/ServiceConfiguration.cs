using GitObjectDb.Api.GraphQL.Graph.Objects;
using GitObjectDb.Api.GraphQL.Queries;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

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

        source
            .AddSingleton<ISchema, Graph.Schema>()
            .AddSingleton<NodeInputDtoTypeEmitter>()
            .AddCompatibleTypesAsSingletons(typeof(CachedResultLoaderBase<,>))
            .AddCompatibleTypesAsSingletons(typeof(IFieldResolver))
            .Configure(configure);

        return source;
    }

    private static IServiceCollection AddCompatibleTypesAsSingletons(this IServiceCollection source, Type compatibleType)
    {
        Func<Type, bool> predicate = compatibleType.IsGenericTypeDefinition ?
            type => (type.BaseType?.IsGenericType ?? false) && type.BaseType.GetGenericTypeDefinition() == compatibleType :
            type => compatibleType.IsAssignableFrom(type);
        foreach (var type in typeof(ServiceConfiguration).Assembly.GetTypes().Where(predicate))
        {
            source.AddSingleton(type);
        }
        return source;
    }

    /// <summary>Gets whether GitObjectDbApi has already been registered.</summary>
    /// <param name="source">The source.</param>
    /// <returns><c>true</c> if the service has already been registered, <c>false</c> otherwise.</returns>
    private static bool IsGitObjectDbGraphQLRegistered(this IServiceCollection source) =>
        source.Any(sd => sd.ServiceType == typeof(ISchema));
}
