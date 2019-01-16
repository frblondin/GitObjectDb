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
#pragma warning disable CA1710 // Identifiers should have correct suffix
    public interface ILazyChildren<TChild> : ILazyChildren, IReadOnlyList<TChild>
#pragma warning restore CA1710 // Identifiers should have correct suffix
        where TChild : class, IModelObject
    {
        /// <summary>
        /// Attaches the instance to its parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The same instance, allowing simpled chained calls if needed.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        ILazyChildren<TChild> AttachToParent(IModelObject parent);
    }

    /// <summary>
    /// Provides support for lazy children loading.
    /// </summary>
#pragma warning disable CA1710 // Identifiers should have correct suffix
#pragma warning disable CA1010 // Collections should implement generic interface
    public interface ILazyChildren : IEnumerable
#pragma warning restore CA1010 // Collections should implement generic interface
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        /// <summary>
        /// Gets the parent.
        /// </summary>
        IModelObject Parent { get; }

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
        ILazyChildren Clone(bool forceVisit, Func<IModelObject, IModelObject> update, IEnumerable<IModelObject> added = null, IEnumerable<IModelObject> deleted = null);
    }
}
