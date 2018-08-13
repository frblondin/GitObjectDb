using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Provides information about a model type.
    /// </summary>
    public interface IModelDataAccessor
    {
        /// <summary>
        /// Gets the type.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets the child properties.
        /// </summary>
        IImmutableList<ChildPropertyInfo> ChildProperties { get; }

        /// <summary>
        /// Gets the modifiable properties.
        /// </summary>
        IImmutableList<ModifiablePropertyInfo> ModifiableProperties { get; }

        /// <summary>
        /// Gets the constructor parameter binding.
        /// </summary>
        ConstructorParameterBinding ConstructorParameterBinding { get; }

#pragma warning disable CA1716 // Identifiers should not match keywords
        /// <summary>
        /// Creates a copy of the instance and apply changes according to the new test values provided in the predicate.
        /// </summary>
        /// <param name="source">The object.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>The newly created copy. Both parents and children nodes have been cloned as well.</returns>
        IMetadataObject With(IMetadataObject source, IPredicateReflector predicate);
#pragma warning restore CA1716 // Identifiers should not match keywords

        /// <summary>
        /// Deep clones an instance and modify children on the fly.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="processArgument">The process argument.</param>
        /// <param name="childChangesGetter">The child changes getter.</param>
        /// <param name="mustForceVisit">The must force visit.</param>
        /// <returns>The cloned instance.</returns>
        IObjectRepository DeepClone(IObjectRepository instance, ProcessArgument processArgument, ChildChangesGetter childChangesGetter, Func<IMetadataObject, bool> mustForceVisit);
    }
}
