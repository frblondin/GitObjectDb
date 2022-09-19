using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Api.Model;
using GitObjectDb.Model;
using GraphQL;
using GraphQL.DI;
using GraphQL.Types;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL;

/// <summary>A set of methods for instances of <see cref="IMvcBuilder"/>.</summary>
public static class MvcBuilderExtensions
{
    /// <summary>Adds support of GraphQL queries.</summary>
    /// <param name="source">The source.</param>
    /// <param name="emitter">The dto type emitter.</param>
    /// <param name="configure">The GraphQL configuration delegate.</param>
    /// <returns>The source <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddGitObjectDbGraphQLControllers(this IMvcBuilder source, DtoTypeEmitter emitter, Action<IGraphQLBuilder>? configure = null)
    {
        // Avoid double-registrations
        if (source.Services.Any(sd => sd.ServiceType == typeof(ISchema)))
        {
            return source;
        }

        var schema = new GitObjectDbSchema(emitter);
        var query = (GitObjectDbQuery)schema.Query;
        source.Services
            .AddSingleton<ISchema>(schema)
            .AddGraphQL(builder =>
            {
                builder
                    .AddSystemTextJson()
                    .AddGraphTypes(query.DtoEmitter.AssemblyBuilder);
                configure?.Invoke(builder);
            });

        source.ConfigureApplicationPartManager(m =>
            m.ApplicationParts.Add(new AssemblyPart(typeof(NodeController).Assembly)));

        return source;
    }
}
