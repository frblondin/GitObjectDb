using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Provides support for lazy children loading.
    /// </summary>
    /// <typeparam name="TChild">The type of the children.</typeparam>
    /// <seealso cref="GitObjectDb.Models.ILazyChildren" />
    /// <seealso cref="System.Collections.Generic.IReadOnlyList{TChild}" />
    public interface ILazyChildren<TChild> : ILazyChildren, IReadOnlyList<TChild>
        where TChild : class, IMetadataObject
    {
        /// <summary>
        /// Attaches the instance to its parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The same instance, allowing simpled chained calls if needed.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        ILazyChildren<TChild> AttachToParent(IMetadataObject parent);
    }

    /// <summary>
    /// Provides support for lazy children loading.
    /// </summary>
    public interface ILazyChildren : IEnumerable
    {
        /// <summary>
        /// Gets the parent.
        /// </summary>
        IMetadataObject Parent { get; }

        /// <summary>
        /// Gets a value indicating whether the children have been loaded.
        /// </summary>
        bool AreChildrenLoaded { get; }

        /// <summary>
        /// Gets a value indicating whether the lazy children should be visited to check for changes.
        /// </summary>
        bool ForceVisit { get; }

        /// <summary>
        /// Clones this instance by applying an update to each child.
        /// </summary>
        /// <param name="forceVisit">if set to <c>true</c> [force visit].</param>
        /// <param name="update">The update.</param>
        /// <param name="added">Nodes that must be added.</param>
        /// <param name="deleted">Nodes that must be deleted.</param>
        /// <returns>The new <see cref="ILazyChildren"/> instance containing the result of the transformations.</returns>
        ILazyChildren Clone(bool forceVisit, Func<IMetadataObject, IMetadataObject> update, IEnumerable<IMetadataObject> added = null, IEnumerable<IMetadataObject> deleted = null);

        /// <summary>
        /// Adds the specified child. This method should only be used within <see cref="IMetadataObjectExtensions.With{TModel}(TModel, Expression{Predicate{TModel}})"/>.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <returns>Return type required to return a predicate.</returns>
        bool Add(IMetadataObject child);

        /// <summary>
        /// Deletes the specified child. This method should only be used within <see cref="IMetadataObjectExtensions.With{TModel}(TModel, Expression{Predicate{TModel}})"/>.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <returns>Return type required to return a predicate.</returns>
        bool Delete(IMetadataObject child);
    }
}
