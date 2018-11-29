using GitObjectDb.Reflection;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Lazy children helper.
    /// </summary>
    internal static class LazyChildrenHelper
    {
        /// <summary>
        /// Creates a new instance of <see cref="ILazyChildren"/> for a <see cref="ChildPropertyInfo"/>.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="factory">The factory.</param>
        /// <returns>The new lazy children instance.</returns>
        internal static ILazyChildren Create(ChildPropertyInfo propertyInfo, Func<IModelObject, IRepository, IEnumerable<IModelObject>> factory)
        {
            var targetType = typeof(LazyChildren<>).MakeGenericType(propertyInfo.ItemType);
            return (ILazyChildren)Activator.CreateInstance(targetType, factory);
        }

        /// <summary>
        /// Tries to get the lazy children interface if any.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type if resolved, <code>null</code> otherwise.</returns>
        internal static Type TryGetLazyChildrenInterface(Type type) =>
            type.GetInterfaces().Prepend(type).FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ILazyChildren<>));
    }
}
