using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Provides support for lazy children loading.
    /// </summary>
    /// <typeparam name="TChild">The type of the children.</typeparam>
    /// <seealso cref="GitObjectDb.Models.ILazyChildren" />
    /// <seealso cref="System.Collections.Generic.IReadOnlyList{TChild}" />
#pragma warning disable CA1710 // Identifiers should have correct suffix
    public interface ILazyChildren<TChild> : ILazyChildren
#pragma warning restore CA1710 // Identifiers should have correct suffix
                            where TChild : class, IModelObject
    {
        /// <summary>
        /// Gets starts the asynchronous factory method, if it has not already started, and returns the resulting task.
        /// </summary>
        Task<IImmutableList<TChild>> Task { get; }

        /// <summary>
        /// Attaches the instance to its parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The same instance, allowing simpled chained calls if needed.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        ILazyChildren<TChild> AttachToParent(IModelObject parent);

        /// <summary>
        /// Asynchronous infrastructure support. This method permits instances of <see cref="ILazyChildren{TChild}"/> to be await'ed.
        /// </summary>
        /// <returns>An object that waits for the completion of an asynchronous task.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        new TaskAwaiter<IImmutableList<TChild>> GetAwaiter();

        /// <summary>
        /// Asynchronous infrastructure support. This method permits instances of <see cref="ILazyChildren{TChild}"/> to be await'ed.
        /// </summary>
        /// <param name="continueOnCapturedContext"><code>true</code> to attempt to marshal the continuation back to the original context captured. <code>false</code> otherwise.</param>
        /// <returns>An awaitable object that enables configured awaits on a task.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        new ConfiguredTaskAwaitable<IImmutableList<TChild>> ConfigureAwait(bool continueOnCapturedContext);
    }

    /// <summary>
    /// Provides support for lazy children loading.
    /// </summary>
#pragma warning disable CA1710 // Identifiers should have correct suffix
#pragma warning disable CA1010 // Collections should implement generic interface
    public interface ILazyChildren
#pragma warning restore CA1010 // Collections should implement generic interface
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        /// <summary>
        /// Gets the parent.
        /// </summary>
        IModelObject Parent { get; }

        /// <summary>
        /// Gets a value indicating whether whether the asynchronous factory method has started.
        /// This is initially <c>false</c> and becomes <c>true</c> when this instance is awaited or after <see cref="Start"/> is called.
        /// </summary>
        bool IsStarted { get; }

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
        ILazyChildren Clone(bool forceVisit, Func<IModelObject, Task<IModelObject>> update, IEnumerable<IModelObject> added = null, IEnumerable<IModelObject> deleted = null);

        /// <summary>
        /// Asynchronous infrastructure support. This method permits instances of <see cref="ILazyChildren{TChild}"/> to be await'ed.
        /// </summary>
        /// <returns>An object that waits for the completion of an asynchronous task.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        TaskAwaiter<IEnumerable<IModelObject>> GetAwaiter();

        /// <summary>
        /// Asynchronous infrastructure support. This method permits instances of <see cref="ILazyChildren{TChild}"/> to be await'ed.
        /// </summary>
        /// <param name="continueOnCapturedContext"><code>true</code> to attempt to marshal the continuation back to the original context captured. <code>false</code> otherwise.</param>
        /// <returns>An awaitable object that enables configured awaits on a task.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        ConfiguredTaskAwaitable<IEnumerable<IModelObject>> ConfigureAwait(bool continueOnCapturedContext);

        /// <summary>
        /// Starts the asynchronous initialization, if it has not already started.
        /// </summary>
        void Start();
    }
}
