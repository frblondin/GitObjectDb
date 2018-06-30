using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using GitObjectDb.Compare;
using GitObjectDb.Reflection;

namespace GitObjectDb
{
    public static class DefaultInterfaceMapping
    {
        public static IEnumerable<(Type Type, Type InterfaceType)> Implementations { get; }
            = GetImplementations().ToImmutableList();
        static IEnumerable<(Type Type, Type InterfaceType)> GetImplementations()
        {
            yield return (typeof(ModelDataAccessorProvider), typeof(IModelDataAccessorProvider));
            yield return (typeof(MostParametersConstructorSelector), typeof(IConstructorSelector));
            yield return (typeof(ComputeTreeChanges), null);
        }

        public static IEnumerable<(Type InterfaceType, Func<object, object> Adapter, bool SingleInstance)> Decorators { get; }
            = GetDecorators().ToImmutableList();
        static IEnumerable<(Type InterfaceType, Func<object, object> Adapter, bool SingleInstance)> GetDecorators()
        {
            yield return (
                typeof(IModelDataAccessorProvider),
                inner => new CachedModelDataAccessorProvider((IModelDataAccessorProvider)inner),
                true);
        }
    }
}
