using GitObjectDb.Api.GraphQL.Model;
using GitObjectDb.Api.Model;
using GitObjectDb.Model;
using GraphQL;
using GraphQL.MicrosoftDI;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL;

/// <summary>A set of methods for instances of <see cref="IMvcBuilder"/>.</summary>
public static class MvcBuilderExtensions
{
    /// <summary>Adds support of GraphQL queries.</summary>
    /// <param name="source">The source.</param>
    /// <param name="model">The <see cref="IDataModel"/> to be exposed through GraphQL.</param>
    /// <returns>The source <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddGitObjectDbGraphQL(this IMvcBuilder source, IDataModel model)
    {
        var query = new GitObjectDbQuery(model);
        source.Services
            .AddGitObjectDbApi()
            .AddAutoMapper(c => c.AddProfile(new AutoMapperProfile(query.DtoEmitter.TypeDescriptions)))
            .AddSingleton<ISchema>(new GitObjectDbSchema(query))
            .AddGraphQL(builder => builder
                .AddMetrics(true)
                .AddSystemTextJson()
                .AddGraphTypes(query.DtoEmitter.AssemblyBuilder));
        return source
            .ConfigureApplicationPartManager(m => m.ApplicationParts.Add(new AssemblyPart(Assembly.GetExecutingAssembly())));
    }
}
