using GitObjectDb.Compare;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
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
        }

        static IEnumerable<(Type InterfaceType, Func<object, object> Adapter, bool SingleInstance)> GetDecorators()
        {
            yield return (
                typeof(IModelDataAccessorProvider),
                inner => new CachedModelDataAccessorProvider((IModelDataAccessorProvider)inner),
                true);
        }
    }
}
