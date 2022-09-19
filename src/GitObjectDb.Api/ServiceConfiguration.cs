using GitObjectDb.Api.Model;
using GitObjectDb.Injection;
using GitObjectDb.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api;

/// <summary>A set of methods for instances of <see cref="IServiceCollection"/>.</summary>
public static class ServiceConfiguration
{
    private static DtoTypeEmitter? _emitter;

    /// <summary>Adds access to GitObjectDb repositories.</summary>
    /// <param name="source">The source.</param>
    /// <returns>The source <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddGitObjectDbApi(this IServiceCollection source)
    {
        // Avoid double-registrations
        if (source.IsGitObjectDbApiRegistered())
        {
            throw new NotSupportedException("GitObjectDbApi has already been registered.");
        }

        if (!source.Any(sd => sd.ServiceType == typeof(IMemoryCache)))
        {
            throw new NotSupportedException($"No {nameof(IMemoryCache)} service registered.");
        }

        var model = source.FirstOrDefault(s => s.ServiceType == typeof(IDataModel) &&
            s.Lifetime == ServiceLifetime.Singleton &&
            s.ImplementationInstance is not null)?.ImplementationInstance as IDataModel ??
            throw new NotSupportedException($"{nameof(IDataModel)} has not bee registered.");

        _emitter = new DtoTypeEmitter(model);
        source
            .AddSingleton<DataProvider>()
            .AddSingleton(_emitter)
            .AddAutoMapper(c => c.AddProfile(new AutoMapperProfile(_emitter.TypeDescriptions)));

        return source;
    }

    /// <summary>Gets whether GitObjectDbApi has already been registered.</summary>
    /// <param name="source">The source.</param>
    /// <returns><c>true</c> if the service has already been registered, <c>false</c> otherwise.</returns>
    private static bool IsGitObjectDbApiRegistered(this IServiceCollection source) =>
        source.Any(sd => sd.ServiceType == typeof(DataProvider)) ||
        source.Any(sd => sd.ServiceType == typeof(DtoTypeEmitter));
}
