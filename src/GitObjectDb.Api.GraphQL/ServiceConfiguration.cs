using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Api.Model;
using GitObjectDb.Injection;
using GitObjectDb.Model;
using GraphQL;
using GraphQL.MicrosoftDI;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL;

/// <summary>A set of methods for instances of <see cref="IServiceCollection"/>.</summary>
public static class ServiceConfiguration
{
    /// <summary>Adds access to GitObjectDb repositories.</summary>
    /// <param name="source">The source.</param>
    /// <param name="model">The <see cref="IDataModel"/> to be exposed through GraphQL.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <param name="emitter">The dto type emitter.</param>
    /// <returns>The source <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddGitObjectDbGraphQL(this IServiceCollection source, IDataModel model, Action<IGitObjectDbBuilder> configure, out DtoTypeEmitter emitter)
    {
        source.AddGitObjectDbApi(model, configure, out emitter);

        // Avoid double-registrations
        if (source.Any(sd => sd.ServiceType == typeof(ISchema)))
        {
            return source;
        }

        var schema = new GitObjectDbSchema(emitter);
        var query = (GitObjectDbQuery)schema.Query;
        source
            .AddSingleton<ISchema>(schema)
            .AddGraphQL(builder => builder
                .UseApolloTracing(true)
                .AddSystemTextJson()
                .AddGraphTypes(query.DtoEmitter.AssemblyBuilder));
        return source;
    }
}
