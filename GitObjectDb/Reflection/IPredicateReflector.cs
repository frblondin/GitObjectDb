using GitObjectDb.Models;
using System;
using System.Collections.Generic;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Analyzes the conditions of a predicate to collect property assignments to be made.
    /// </summary>
    public interface IPredicateReflector
    {
        /// <summary>
        /// Returns the value collected by the reflector, <paramref name="fallback"/> is none was found.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="argumentType">The argument type.</param>
        /// <param name="fallback">The fallback value.</param>
        /// <returns>The final value to be used for the parameter.</returns>
        object ProcessArgument(IMetadataObject instance, string name, Type argumentType, object fallback = null);

        /// <summary>
        /// Gets the child changes collected by the reflector.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="childProperty">The child property.</param>
        /// <returns>A list of changes.</returns>
        (IEnumerable<IMetadataObject> Additions, IEnumerable<IMetadataObject> Deletions) GetChildChanges(IMetadataObject instance, ChildPropertyInfo childProperty);
    }
}