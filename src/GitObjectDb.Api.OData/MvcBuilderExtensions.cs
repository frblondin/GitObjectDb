using GitObjectDb.Api.Model;
using GitObjectDb.Api.OData.Model;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.OData;

/// <summary>A set of methods for instances of <see cref="IMvcBuilder"/>.</summary>
public static class MvcBuilderExtensions
{
    /// <summary>Adds support of OData queries.</summary>
    /// <param name="source">The source.</param>
    /// <param name="routePrefix">The route prefix.</param>
    /// <param name="setupAction">The OData options to configure the services with, including access
    /// to a service provider which you can resolve services from.</param>
    /// 
    /// <returns>The source <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddGitObjectDbODataControllers(this IMvcBuilder source,
        string routePrefix, Action<ODataOptions> setupAction)
    {
        var emitter = source.Services.FirstOrDefault(s => s.ServiceType == typeof(DtoTypeEmitter) &&
            s.Lifetime == ServiceLifetime.Singleton &&
            s.ImplementationInstance is not null)?.ImplementationInstance as DtoTypeEmitter ??
            throw new NotSupportedException($"GitObjectDbApi has not bee registered.");

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
