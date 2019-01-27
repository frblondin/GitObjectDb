using GitObjectDb.Models.Compare;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Represents an index that improves the speed of data retrieval.
    /// </summary>
#pragma warning disable CA1710 // Identifiers should have correct suffix
    public interface IObjectRepositoryIndex : IModelObject
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        /// <summary>
        /// Gets the indexed values.
        /// </summary>
        ImmutableSortedDictionary<string, ImmutableSortedSet<string>> Values { get; }

        /// <summary>
        /// Gets the number of key/value collection pairs in the index.
        /// </summary>
        /// <returns>The number of key/value collection pairs in the index.</returns>
        int Count { get; }

        /// <summary>
        /// Gets the <see cref="IEnumerable{IModelObject}" /> sequence of values indexed by a specified key.
        /// </summary>
        /// <param name="key">The key of the desired sequence of values.</param>
        /// <returns>The <see cref="IEnumerable{IModelObject}" /> sequence of values indexed by the specified key.</returns>
        IEnumerable<IModelObject> this[string key] { get; }

        /// <summary>
        /// Determines whether a specified key exists in the index.
        /// </summary>
        /// <param name="key">The key to search for in the index.</param>
        /// <returns><see langword="true" /> if <paramref name="key" /> is in the index; otherwise, <see langword="false" />.</returns>
        bool Contains(string key);

        /// <summary>
        /// Updates the index from the given changes.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <returns>The updated indexed values.</returns>
        ImmutableSortedDictionary<string, ImmutableSortedSet<string>> Update(ObjectRepositoryChangeCollection changes);

        /// <summary>
        /// Goes through all repository objects and recomputes the index.
        /// </summary>
        /// <returns>The indexed values.</returns>
        ImmutableSortedDictionary<string, ImmutableSortedSet<string>> FullScan();
    }
}
