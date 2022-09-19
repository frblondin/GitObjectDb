using GitObjectDb.Api.Model;
using GitObjectDb.Api.OData.Model;
using GitObjectDb.Model;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.OData;

/// <summary>A set of methods for instances of <see cref="IMvcBuilder"/>.</summary>
public static class MvcBuilderExtensions
{
    /// <summary>Adds support of OData queries.</summary>
    /// <param name="source">The source.</param>
    /// <param name="routePrefix">The route prefix.</param>
    /// <param name="emitter">The dto type emitter.</param>
    /// <param name="setupAction">The OData options to configure the services with, including access
    /// to a service provider which you can resolve services from.</param>
    /// <returns>The source <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddGitObjectDbODataControllers(this IMvcBuilder source,
        string routePrefix, DtoTypeEmitter emitter, Action<ODataOptions> setupAction)
    {
        return source
            .ConfigureApplicationPartManager(m =>
            {
                m.ApplicationParts.Add(new GeneratedTypesApplicationPart(emitter));
            })
            .AddOData(options =>
            {
                var dtoTypes = emitter.TypeDescriptions.Select(d => d.DtoType);
                var edmModel = emitter.Model.ConvertToEdm(dtoTypes);
                options.AddRouteComponents(routePrefix, edmModel);
                setupAction(options);
            });
    }
}
