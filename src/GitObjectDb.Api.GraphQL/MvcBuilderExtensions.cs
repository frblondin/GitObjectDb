using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.DI;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using ServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;

namespace GitObjectDb.Api.GraphQL;

/// <summary>A set of methods for instances of <see cref="IMvcBuilder"/>.</summary>
public static class MvcBuilderExtensions
{
    /// <summary>Adds support of GraphQL queries.</summary>
    /// <param name="source">The source.</param>
    /// <param name="configure">The GraphQL configuration delegate.</param>
    /// <returns>The source <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddGitObjectDbGraphQLControllers(this IMvcBuilder source, Action<IGraphQLBuilder>? configure = null)
    {
        var emitter = source.Services.FirstOrDefault(s => s.ServiceType == typeof(DtoTypeEmitter) &&
            s.Lifetime == ServiceLifetime.Singleton &&
            s.ImplementationInstance is not null)?.ImplementationInstance as DtoTypeEmitter ??
            throw new NotSupportedException($"GitObjectDbApi has not bee registered.");

        source.Services
            .AddGraphQL(builder =>
            {
                builder.AddGraphTypes(emitter.AssemblyBuilder);
                configure?.Invoke(builder);
            });

        source.ConfigureApplicationPartManager(m =>
            m.ApplicationParts.Add(new AssemblyPart(typeof(NodeController).Assembly)));

        return source;
    }
}
