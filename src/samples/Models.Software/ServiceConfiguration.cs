using GitObjectDb.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Models.Software
{
    /// <summary>
    /// A set of methods for instances of <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddSoftwareModel(this IServiceCollection source) =>
            source.AddSingleton(_ => new ConventionBaseModelBuilder().RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).Build());
    }
}
