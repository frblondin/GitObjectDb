using GitObjectDb.Comparison;
using GitObjectDb.Internal;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Internal.Queries;
using GitObjectDb.Serialization;
using GitObjectDb.Serialization.Json;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace GitObjectDb
{
    /// <summary>A set of methods for instances of <see cref="IServiceCollection"/>.</summary>
    public static class ServiceConfiguration
    {
        /// <summary>Adds access to GitObjectDb repositories.</summary>
        /// <param name="source">The source.</param>
        /// <returns>The source <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddGitObjectDb(this IServiceCollection source) =>
            ConfigureServices(source);

        private static IServiceCollection ConfigureServices(IServiceCollection source)
        {
            ConfigureMain(source);
            ConfigureQueries(source);
            ConfigureCommands(source);

            return source;
        }

        private static void ConfigureMain(IServiceCollection source)
        {
            source.AddFactoryDelegate<ConnectionFactory, Connection>();
            var internalFactories = typeof(Factories)
                .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .Where(t => typeof(Delegate).IsAssignableFrom(t))
                .ToArray();
            source.AddFactoryDelegates(Assembly.GetExecutingAssembly(), internalFactories);
            source.AddSingleton<INodeSerializer, DefaultSerializer>();
            source.AddSingleton<NodeSerializerCache>();
            source.AddSingleton<Comparer>();
            source.AddSingleton<IComparer>(s => s.GetRequiredService<Comparer>());
            source.AddSingleton<IComparerInternal>(s => s.GetRequiredService<Comparer>());
            source.AddSingleton<ITreeValidation, TreeValidation>();
        }

        private static void ConfigureQueries(IServiceCollection source)
        {
            source.AddServicesImplementing(typeof(IQuery<,>), ServiceLifetime.Singleton);
        }

        private static void ConfigureCommands(IServiceCollection source)
        {
            source.AddSingleton<UpdateTreeCommand>();
            source.AddSingleton<CommitCommand>();
        }
    }
}
