using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.Migrations;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
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
            source.AddSingleton<Func<RepositoryDescription, IComputeTreeChanges>>(s =>
                description => new ComputeTreeChanges(s, description));
            source.AddSingleton<IRepositoryFactory, RepositoryFactory>();
            source.AddSingleton<IRepositoryProvider, RepositoryProvider>();
            source.AddSingleton<IModelDataAccessorProvider>(s =>
                new CachedModelDataAccessorProvider(new ModelDataAccessorProvider(s)));
            source.AddSingleton<Func<RepositoryDescription, IObjectRepository, string, IMetadataTreeMerge>>(s =>
                (description, repository, branchName) => new MetadataTreeMerge(s, description, repository, branchName));
            source.AddSingleton<Func<RepositoryDescription, MigrationScaffolder>>(s =>
                description => new MigrationScaffolder(s, description));
            return source;
        }
    }
}
