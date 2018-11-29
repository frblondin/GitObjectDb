using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Provides support for lazy link to another <see cref="IModelObject"/>.
    /// </summary>
    /// <typeparam name="TLink">The type of the link.</typeparam>
    public interface ILazyLink<out TLink> : ILazyLink
        where TLink : class, IModelObject
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        new TLink Link { get; }
    }

    /// <summary>
    /// Provides support for lazy link to another <see cref="IModelObject"/>.
    /// </summary>
    public interface ILazyLink : ICloneable
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        IModelObject Link { get; }

        /// <summary>
        /// Gets the path.
        /// </summary>
        ObjectPath Path { get; }

        /// <summary>Gets a value indicating whether the link value has been created.</summary>
        /// <returns><code>true</code> if a value has been created.</returns>
        bool IsLinkCreated { get; }
    }
}
