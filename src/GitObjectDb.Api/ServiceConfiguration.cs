using GitObjectDb.Api.Model;
using GitObjectDb.Injection;
using GitObjectDb.Model;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api;

/// <summary>A set of methods for instances of <see cref="IServiceCollection"/>.</summary>
public static class ServiceConfiguration
{
    private static DtoTypeEmitter? _emitter;

    /// <summary>Adds access to GitObjectDb repositories.</summary>
    /// <param name="source">The source.</param>
    /// <param name="model">The <see cref="IDataModel"/> to be exposed.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <param name="emitter">The dto type emitter.</param>
    /// <returns>The source <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddGitObjectDbApi(this IServiceCollection source, IDataModel model, Action<IGitObjectDbBuilder> configure, out DtoTypeEmitter emitter)
    {
        source.AddGitObjectDb(configure);

        // Avoid double-registrations
        if (source.Any(sd => sd.ServiceType == typeof(DataProvider)))
        {
            emitter = _emitter ?? throw new NotSupportedException("Emitter should have been created.");
            return source;
        }

        source
            .AddScoped<DataProvider>()
            .AddSingleton(_emitter = emitter = new DtoTypeEmitter(model))
            .AddAutoMapper(c => c.AddProfile(new AutoMapperProfile(_emitter.TypeDescriptions)));

        return source;
    }
}
