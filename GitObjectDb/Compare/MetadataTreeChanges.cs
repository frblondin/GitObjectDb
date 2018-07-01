using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace GitObjectDb.Compare
{
    /// <summary>
    /// Holds the result of a diff between two trees.
    /// </summary>
    [DebuggerDisplay("+{Added.Count} ~{Modified.Count} -{Deleted.Count}")]
    public class MetadataTreeChanges
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataTreeChanges"/> class.
        /// </summary>
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
        public MetadataTreeChanges(IImmutableList<MetadataTreeEntryChanges> added, IImmutableList<MetadataTreeEntryChanges> modified, IImmutableList<MetadataTreeEntryChanges> deleted)
        {
            Modified = modified ?? throw new ArgumentNullException(nameof(modified));
            Added = added ?? throw new ArgumentNullException(nameof(added));
            Deleted = deleted ?? throw new ArgumentNullException(nameof(deleted));
        }

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
    }
}
