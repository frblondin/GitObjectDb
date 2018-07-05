using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace GitObjectDb
{
    /// <summary>
    /// Provides all default service mappings so that dependency injection libraries
    /// can know which types must be registered.
    /// </summary>
    public static class DefaultServiceMapping
    {
        /// <summary>
        /// Gets the implementations and their interface type (if any).
        /// </summary>
        public static IEnumerable<(Type Type, Type InterfaceType)> Implementations { get; }
            = GetImplementations().ToImmutableList();

        /// <summary>
        /// Gets the decorators.
        /// </summary>
        public static IEnumerable<(Type InterfaceType, Func<object, object> Adapter, bool SingleInstance)> Decorators { get; }
            = GetDecorators().ToImmutableList();

        static IEnumerable<(Type Type, Type InterfaceType)> GetImplementations()
        {
            yield return (typeof(InstanceLoader), typeof(IInstanceLoader));
            yield return (typeof(ModelDataAccessorProvider), typeof(IModelDataAccessorProvider));
            yield return (typeof(MostParametersConstructorSelector), typeof(IConstructorSelector));
            yield return (typeof(ComputeTreeChanges), typeof(IComputeTreeChanges));
            yield return (typeof(RepositoryFactory), typeof(IRepositoryFactory));
            yield return (typeof(RepositoryProvider), typeof(IRepositoryProvider));
        }

        static IEnumerable<(Type InterfaceType, Func<object, object> Adapter, bool SingleInstance)> GetDecorators()
        {
            yield return (
                typeof(IModelDataAccessorProvider),
                inner => new CachedModelDataAccessorProvider((IModelDataAccessorProvider)inner),
                true);
        }

        /// <summary>
        /// Adds access to GitObjectDb repositories.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>The source <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddGitObjectDb(this IServiceCollection source)
        {
            source.AddSingleton<IInstanceLoader, InstanceLoader>();
            source.AddSingleton<IModelDataAccessorProvider, ModelDataAccessorProvider>();
            source.AddSingleton<IConstructorSelector, MostParametersConstructorSelector>();
            source.AddSingleton<Func<RepositoryDescription, IComputeTreeChanges>>(s =>
                description => new ComputeTreeChanges(s, description));
            source.AddSingleton<IRepositoryFactory, RepositoryFactory>();
            source.AddSingleton<IRepositoryProvider, RepositoryProvider>();
            source.AddSingleton<IModelDataAccessorProvider>(s =>
                new CachedModelDataAccessorProvider(new ModelDataAccessorProvider(s)));
            return source;
        }
    }
}
