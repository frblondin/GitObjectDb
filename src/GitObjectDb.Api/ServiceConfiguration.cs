using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api;

/// <summary>A set of methods for instances of <see cref="IServiceCollection"/>.</summary>
public static class ServiceConfiguration
{
    /// <summary>Adds access to GitObjectDb repositories.</summary>
    /// <param name="source">The source.</param>
    /// <returns>The source <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddGitObjectDbApi(this IServiceCollection source) =>
        ConfigureServices(source);

    private static IServiceCollection ConfigureServices(IServiceCollection source)
    {
        source.AddScoped<DataProvider>();
        return source;
    }
}
