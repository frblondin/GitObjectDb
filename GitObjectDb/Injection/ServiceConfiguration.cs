using GitObjectDb.Commands;
using GitObjectDb.Internal;
using GitObjectDb.Queries;
using GitObjectDb.Validations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace GitObjectDb
{
    /// <summary>
    /// A set of methods for instances of <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceConfiguration
    {
        /// <summary>
        /// Adds access to GitObjectDb repositories.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>The source <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddGitObjectDb(this IServiceCollection source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return ConfigureServices(source);
        }

        private static IServiceCollection ConfigureServices(IServiceCollection source)
        {
            ConfigureMain(source);

            return source;
        }

        private static void ConfigureMain(IServiceCollection source)
        {
            source.AddFactoryDelegate<ConnectionFactory, Connection>();
            var internalFactories = typeof(Factories)
                .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .Where(t => typeof(Delegate).IsAssignableFrom(t));
            source.AddFactoryDelegates(internalFactories);
            source.AddServicesImplementing(typeof(IQuery<,>), ServiceLifetime.Singleton);
            source.AddServicesImplementing(typeof(IQuery<,,>), ServiceLifetime.Singleton);
            source.AddSingleton<ITreeValidation, TreeValidation>();
            source.AddSingleton<CommitCommand>();
        }
    }
}
