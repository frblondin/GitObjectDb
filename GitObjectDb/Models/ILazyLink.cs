using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Provides support for lazy link to another <see cref="IMetadataObject"/>.
    /// </summary>
    /// <typeparam name="TLink">The type of the link.</typeparam>
    public interface ILazyLink<out TLink> : ILazyLink
        where TLink : class, IMetadataObject
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        new TLink Link { get; }

        /// <summary>
        /// Attaches the instance to its parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The same instance, allowing simpled chained calls if needed.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        ILazyLink<TLink> AttachToParent(IMetadataObject parent);
    }

    /// <summary>
    /// Provides support for lazy link to another <see cref="IMetadataObject"/>.
    /// </summary>
    public interface ILazyLink : ICloneable
    {
        /// <summary>
        /// Gets the parent.
        /// </summary>
        IMetadataObject Parent { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        IMetadataObject Link { get; }

        /// <summary>
        /// Gets the path.
        /// </summary>
        string Path { get; }

        /// <summary>Gets a value indicating whether the link value has been created.</summary>
        /// <returns><code>true</code> if a value has been created.</returns>
        bool IsLinkCreated { get; }
    }
}
