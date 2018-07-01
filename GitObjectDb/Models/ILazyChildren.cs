using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

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

        /// <summary>
        /// Clones this instance by applying an update to each child.
        /// </summary>
        /// <param name="update">The update.</param>
        /// <param name="forceVisit">if set to <c>true</c> [force visit].</param>
        /// <returns>The new <see cref="ILazyChildren{TChild}"/> instance containing the result of the transformations.</returns>
        ILazyChildren<TChild> Clone(Func<TChild, TChild> update, bool forceVisit);
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
        /// <param name="update">The update.</param>
        /// <param name="forceVisit">if set to <c>true</c> [force visit].</param>
        /// <returns>The new <see cref="ILazyChildren"/> instance containing the result of the transformations.</returns>
        ILazyChildren Clone(Func<IMetadataObject, IMetadataObject> update, bool forceVisit);
    }
}
