using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Metadata tree node.
    /// </summary>
    public interface IMetadataObject
    {
        /// <summary>
        /// Gets the parent instance.
        /// </summary>
        IInstance Instance { get; }

        /// <summary>
        /// Gets the parent.
        /// </summary>
        IMetadataObject Parent { get; }

        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the children.
        /// </summary>
        IEnumerable<IMetadataObject> Children { get; }

        /// <summary>
        /// Attaches to instance to a given parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void AttachToParent(IMetadataObject parent);

        /// <summary>
        /// Creates a copy of the instance and apply changes according to the new test values provided in the predicate.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns>The newly created copy. Both parents and children nodes have been cloned as well.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CA1716 // Identifiers should not match keywords
        IMetadataObject With(Expression predicate);
#pragma warning restore CA1716 // Identifiers should not match keywords
    }
}
