using FluentValidation;
using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.JsonConverters;
using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Models.Rebase;
using GitObjectDb.Reflection;
using GitObjectDb.Services;
using GitObjectDb.Validations;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

        static IServiceCollection ConfigureServices(IServiceCollection source)
        {
            source.AddSingleton<GitHooks>();
            source.AddSingleton<IObjectRepositoryLoader, ObjectRepositoryLoader>();
            source.AddSingleton<IModelDataAccessorProvider, ModelDataAccessorProvider>();
            source.AddSingleton<IConstructorSelector, MostParametersConstructorSelector>();
            source.AddFactoryDelegate<ComputeTreeChangesFactory, ComputeTreeChanges>();
            source.AddSingleton<IRepositoryFactory, RepositoryFactory>();
            source.AddSingleton<IRepositoryProvider, RepositoryProvider>();
            source.AddSingleton<IModelDataAccessorProvider>(s =>
                new CachedModelDataAccessorProvider(new ModelDataAccessorProvider(s)));
            source.AddFactoryDelegate<ObjectRepositoryMergeFactory, ObjectRepositoryMerge>();
            source.AddFactoryDelegate<MigrationScaffolderFactory, MigrationScaffolder>();
            source.AddSingleton<IValidatorFactory, ValidatorFactory>();
            source.AddSingleton<JsonSerializationValidator>();
            source.AddSingleton<IObjectRepositorySearch, ObjectRepositorySearch>();
            source.AddFactoryDelegate<ObjectRepositoryRebaseFactory, ObjectRepositoryRebase>();
            AddCreatorParameterResolvers(source);
            return source;
        }

        static void AddCreatorParameterResolvers(IServiceCollection source)
        {
            source.AddSingleton<ICreatorParameterResolver, CreatorParameterChildrenResolver>();
            source.AddSingleton<ICreatorParameterResolver, CreatorParameterFromServiceProviderResolver>();
            source.AddSingleton<ICreatorParameterResolver, CreatorRepositoryContainerParameterResolver>();
        }
    }
}
