using GitObjectDb.Models;
using LibGit2Sharp;
using System;
using System.Diagnostics;

namespace GitObjectDb.Models.Compare
{
    /// <summary>
    /// Holds the changes between two versions of a tree entry.
    /// </summary>
    [DebuggerDisplay("Status = {Status}, Old = {Old?.Id}, New = {New?.Id}")]
    public class ObjectRepositoryEntryChanges
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryEntryChanges"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="status">The change type.</param>
        /// <param name="old">The old value.</param>
        /// <param name="new">The new value.</param>
        public ObjectRepositoryEntryChanges(string path, ChangeKind status, IModelObject old = null, IModelObject @new = null)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Status = status;

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
        public IModelObject Old { get; }

        /// <summary>
        /// Gets the new object.
        /// </summary>
        public IModelObject New { get; }

        /// <summary>
        /// Gets the new path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the type of change.
        /// </summary>
        public ChangeKind Status { get; }
    }
}
