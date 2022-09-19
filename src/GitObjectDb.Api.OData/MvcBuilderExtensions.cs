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
    /// <param name="model">The <see cref="IDataModel"/> to be exposed through OData.</param>
    /// <param name="setupAction">The OData options to configure the services with, including access
    /// to a service provider which you can resolve services from.</param>
    /// <returns>The source <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddGitObjectDbOData(this IMvcBuilder source, string routePrefix, IDataModel model, Action<ODataOptions> setupAction)
    {
        var applicationPart = new GeneratedTypesApplicationPart(model);
        source.Services
            .AddGitObjectDbApi()
            .AddAutoMapper(c => c.AddProfile(new AutoMapperProfile(applicationPart.TypeDescriptions)));
        return source
            .ConfigureApplicationPartManager(m =>
            {
                m.ApplicationParts.Add(applicationPart);
            })
            .AddOData(options =>
            {
                options.AddRouteComponents(routePrefix, EdmModelConverter.ConvertToEdm(model, applicationPart.TypeDescriptions.Select(d => d.DtoType)));
                setupAction(options);
            });
    }
}
