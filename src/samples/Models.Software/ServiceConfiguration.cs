using GitObjectDb.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Models.Software;

/// <summary>A set of methods for instances of <see cref="IServiceCollection"/>.</summary>
public static class ServiceConfiguration
{
    public static IServiceCollection AddSoftwareModel(this IServiceCollection source, out IDataModel model)
    {
        var captured = model = CreateModel();
        return source.AddSingleton(_ => captured);
    }

    public static IServiceCollection AddSoftwareModel(this IServiceCollection source) =>
        source.AddSingleton(_ => CreateModel());

    private static IDataModel CreateModel() => new ConventionBaseModelBuilder()
        .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
        .Build();
}
