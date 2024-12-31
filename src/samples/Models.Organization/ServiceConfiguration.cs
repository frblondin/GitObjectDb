using GitObjectDb.Model;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using System.Reflection;

namespace Models.Organization;

/// <summary>A set of methods for instances of <see cref="IServiceCollection"/>.</summary>
public static class ServiceConfiguration
{
    public static IServiceCollection AddOrganizationModel(this IServiceCollection source) =>
        source.AddSingleton(CreateModel());

    private static IDataModel CreateModel() => new ConventionBaseModelBuilder()
        .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
        .Build();
}
