using GitObjectDb.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace GitObjectDb.Compare
{
    /// <summary>
    /// Holds the result of a diff between two trees.
    /// </summary>
    [DebuggerDisplay("+{Added.Count} ~{Modified.Count} -{Deleted.Count}")]
    public class MetadataTreeChanges : IEnumerable<MetadataTreeEntryChanges>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataTreeChanges"/> class.
        /// </summary>
        /// <param name="oldInstance">The old instance.</param>
        /// <param name="newInstance">The new instance.</param>
        /// <param name="added">The list of <see cref="MetadataTreeEntryChanges" /> that have been been added.</param>
        /// <param name="modified">The list of <see cref="MetadataTreeEntryChanges" /> that have been been modified.</param>
        /// <param name="deleted">The list of <see cref="MetadataTreeEntryChanges" /> that have been been deleted.</param>
        /// <exception cref="ArgumentNullException">
        /// modified
        /// or
        /// added
        /// or
        /// deleted
        /// </exception>
        public MetadataTreeChanges(IInstance oldInstance, IInstance newInstance, IImmutableList<MetadataTreeEntryChanges> added, IImmutableList<MetadataTreeEntryChanges> modified, IImmutableList<MetadataTreeEntryChanges> deleted)
        {
            OldInstance = oldInstance ?? throw new ArgumentNullException(nameof(oldInstance));
            NewInstance = newInstance ?? throw new ArgumentNullException(nameof(newInstance));
            Modified = modified ?? throw new ArgumentNullException(nameof(modified));
            Added = added ?? throw new ArgumentNullException(nameof(added));
            Deleted = deleted ?? throw new ArgumentNullException(nameof(deleted));
        }

        /// <summary>
        /// Gets the old instance.
        /// </summary>
        public IInstance OldInstance { get; }

        /// <summary>
        /// Gets the new instance.
        /// </summary>
        public IInstance NewInstance { get; }

        /// <summary>
        /// Gets the list of <see cref="MetadataTreeEntryChanges" /> that have been been added.
        /// </summary>
        public IImmutableList<MetadataTreeEntryChanges> Added { get; }

        /// <summary>
        /// Gets the list of <see cref="MetadataTreeEntryChanges" /> that have been been modified.
        /// </summary>
        public IImmutableList<MetadataTreeEntryChanges> Modified { get; }

        /// <summary>
        /// Gets the list of <see cref="MetadataTreeEntryChanges" /> that have been been deleted.
        /// </summary>
        public IImmutableList<MetadataTreeEntryChanges> Deleted { get; }

        /// <inheritdoc/>
        public IEnumerator<MetadataTreeEntryChanges> GetEnumerator() =>
            Added.Concat(Modified).Concat(Deleted).GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
