using GitObjectDb.Api.Model;
using GitObjectDb.Injection;
using GitObjectDb.Model;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL;

/// <summary>A set of methods for instances of <see cref="IServiceCollection"/>.</summary>
public static class ServiceConfiguration
{
    /// <summary>Adds support of OData queries.</summary>
    /// <param name="source">The source.</param>
    /// <param name="model">The <see cref="IDataModel"/> to be exposed through GraphQL.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <param name="emitter">The dto type emitter.</param>
    /// <returns>The source <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddGitObjectDbOData(this IServiceCollection source, IDataModel model, Action<IGitObjectDbBuilder> configure, out DtoTypeEmitter emitter)
    {
        return source
            .AddGitObjectDbApi(model, configure, out emitter);
    }
}
