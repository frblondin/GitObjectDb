using GraphQL;
using GraphQL.DI;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL;

/// <summary>A set of methods for instances of <see cref="IMvcBuilder"/>.</summary>
public static class MvcBuilderExtensions
{
    /// <summary>Adds support of GraphQL queries.</summary>
    /// <param name="source">The source.</param>
    /// <param name="configure">The GraphQL configuration delegate.</param>
    /// <returns>The source <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddGitObjectDbGraphQLControllers(this IMvcBuilder source)
    {
        source.ConfigureApplicationPartManager(m =>
            m.ApplicationParts.Add(new AssemblyPart(typeof(NodeController).Assembly)));

        return source;
    }
}
