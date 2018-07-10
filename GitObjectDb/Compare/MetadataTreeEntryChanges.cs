using GitObjectDb.Models;
using LibGit2Sharp;
using System;
using System.Diagnostics;

namespace GitObjectDb.Compare
{
    /// <summary>
    /// Holds the changes between two versions of a tree entry.
    /// </summary>
    [DebuggerDisplay("Status = {Status}, Old = {Old?.Id}, New = {New?.Id}")]
    public class MetadataTreeEntryChanges
    {
        readonly TreeEntryChanges _entryChanges;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataTreeEntryChanges"/> class.
        /// </summary>
        /// <param name="entryChanges">The entry changes.</param>
        /// <param name="old">The old value.</param>
        /// <param name="new">The new value.</param>
        /// <exception cref="ArgumentNullException">old</exception>
        public MetadataTreeEntryChanges(TreeEntryChanges entryChanges, IMetadataObject old = null, IMetadataObject @new = null)
        {
            _entryChanges = entryChanges ?? throw new ArgumentNullException(nameof(entryChanges));
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

        /// <summary>
        /// Gets the new path.
        /// </summary>
        public string Path => _entryChanges.Path;

        /// <summary>
        /// Gets the type of change.
        /// </summary>
        public ChangeKind Status => _entryChanges.Status;
    }
}
