using GitObjectDb.Models;
using System;
using System.Diagnostics;

namespace GitObjectDb.Compare
{
    /// <summary>
    /// Holds the changes between two versions of a tree entry.
    /// </summary>
    [DebuggerDisplay("Old = {Old.Id}, New = {New.Id}")]
    public class MetadataTreeEntryChanges
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataTreeEntryChanges"/> class.
        /// </summary>
        /// <param name="old">The old value.</param>
        /// <param name="new">The new value.</param>
        /// <exception cref="ArgumentNullException">old</exception>
        public MetadataTreeEntryChanges(IMetadataObject old, IMetadataObject @new)
        {
            if (old == null && @new == null)
            {
                throw new ArgumentNullException($"{nameof(old)} and {nameof(@new)}");
            }

            Old = old;
            New = @new;
        }

        /// <summary>
        /// Gets the old object.
        /// </summary>
        public IMetadataObject Old { get; }

        /// <summary>
        /// Gets the new object.
        /// </summary>
        public IMetadataObject New { get; }
    }
}
