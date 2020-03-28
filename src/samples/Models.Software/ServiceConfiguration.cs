using GitObjectDb.Comparison;
using GitObjectDb.Internal;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Internal.Queries;
using GitObjectDb.Model;
using GitObjectDb.Serialization;
using GitObjectDb.Serialization.Json;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
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
