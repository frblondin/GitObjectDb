using FluentValidation;
using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.JsonConverters;
using GitObjectDb.Migrations;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
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
            source.AddSingleton<Func<IObjectRepositoryContainer, RepositoryDescription, IComputeTreeChanges>>(s =>
                (container, description) => new ComputeTreeChanges(s, container, description));
            source.AddSingleton<IRepositoryFactory, RepositoryFactory>();
            source.AddSingleton<IRepositoryProvider, RepositoryProvider>();
            source.AddSingleton<IModelDataAccessorProvider>(s =>
                new CachedModelDataAccessorProvider(new ModelDataAccessorProvider(s)));
            source.AddSingleton<Func<IObjectRepositoryContainer, RepositoryDescription, IObjectRepository, string, IMetadataTreeMerge>>(s =>
                (container, description, repository, branchName) => new MetadataTreeMerge(s, container, description, repository, branchName));
            source.AddSingleton<Func<IObjectRepositoryContainer, RepositoryDescription, MigrationScaffolder>>(s =>
                (container, description) => new MigrationScaffolder(s, container, description));
            source.AddSingleton<IValidatorFactory, ValidatorFactory>();
            source.AddSingleton<JsonSerializationValidator>();
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
