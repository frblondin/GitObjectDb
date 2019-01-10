using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.Models;
using GitObjectDb.Models.Merge;
using GitObjectDb.Reflection;
using GitObjectDb.Services;
using GitObjectDb.Validations;
using GitObjectDb.Models.Rebase;
using GitObjectDb.Validations.PropertyValidators;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using GitObjectDb.Serialization;

namespace Microsoft.Extensions.DependencyInjection
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
            ConfigureInternalServices(source);
            ConfigureReflectionServices(source);
            ConfigureGitServices(source);
            ConfigureSerializationServices(source);
            ConfigureValidationServices(source);
            ConfigureModelServices(source);

            return source;
        }

        private static void ConfigureInternalServices(IServiceCollection source)
        {
            source.AddSingleton<IObjectRepositoryLoader, ObjectRepositoryLoader>();
            source.AddFactoryDelegate<ComputeTreeChangesFactory, ComputeTreeChanges>();
            source.AddFactoryDelegate<MigrationScaffolderFactory, MigrationScaffolder>();
            source.AddSingleton<IObjectRepositorySearch, ObjectRepositorySearch>();
            source.AddFactoryDelegate<MergeProcessor>();
            source.AddFactoryDelegate<RebaseProcessor>();
        }

        private static void ConfigureReflectionServices(IServiceCollection source)
        {
            source.AddSingleton<IModelDataAccessorProvider, ModelDataAccessorProvider>();
            source.AddSingleton<IConstructorSelector, MostParametersConstructorSelector>();
            source.AddSingleton<IModelDataAccessorProvider>(s =>
                new CachedModelDataAccessorProvider(new ModelDataAccessorProvider(s.GetRequiredService<ModelDataAccessorFactory>())));
            source.AddFactoryDelegate<ConstructorParameterBinding>();
            source.AddFactoryDelegate<ModelDataAccessorFactory, ModelDataAccessor>();
        }

        private static void ConfigureGitServices(IServiceCollection source)
        {
            source.AddSingleton<IRepositoryFactory, RepositoryFactory>();
            source.AddSingleton<IRepositoryProvider, RepositoryProvider>();
            source.AddSingleton<GitHooks>();
        }

        private static void ConfigureSerializationServices(IServiceCollection source)
        {
            // Default serializer is Json, can be overridden
            source.AddFactoryDelegate<ObjectRepositorySerializerFactory, GitObjectDb.Serialization.Json.JsonRepositorySerializer>();
            source.AddSingleton<GitObjectDb.Serialization.Json.Converters.ModelObjectContractCache>();
            source.AddSingleton<GitObjectDb.Serialization.Json.Converters.ModelObjectSpecialValueProvider>();
        }

        private static void ConfigureValidationServices(IServiceCollection source)
        {
            source.AddSingleton<IPropertyValidator, DependencyPropertyValidator>();
            source.AddSingleton<IPropertyValidator, LazyLinkPropertyValidator>();
            source.AddSingleton<IPropertyValidator, ObjectPathPropertyValidator>();
            source.AddSingleton<IValidator, Validator>();
        }

        private static void ConfigureModelServices(IServiceCollection source)
        {
            source.AddSingleton<IObjectRepositoryContainerFactory, ObjectRepositoryContainerFactory>();
            source.AddFactoryDelegate<ObjectRepositoryMergeFactory, ObjectRepositoryMerge>();
            source.AddFactoryDelegate<ObjectRepositoryRebaseFactory, ObjectRepositoryRebase>();
        }
    }
}
