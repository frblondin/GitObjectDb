using GitObjectDb.Models;
using GitObjectDb.Serialization;
using GitObjectDb.Services;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models.Compare
{
    /// <summary>
    /// Holds the result of a diff between two trees.
    /// </summary>
    [DebuggerDisplay("+{Added.Count} ~{Modified.Count} -{Deleted.Count}")]
    public class ObjectRepositoryChanges : IReadOnlyList<ObjectRepositoryEntryChanges>
    {
        private readonly IImmutableList<ObjectRepositoryEntryChanges> _changes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryChanges"/> class.
        /// </summary>
        /// <param name="newRepository">The new repository.</param>
        /// <param name="changes">The list of <see cref="ObjectRepositoryEntryChanges" /> that have been been changed.</param>
        /// <param name="oldRepository">The old repository.</param>
        internal ObjectRepositoryChanges(IObjectRepository newRepository, IImmutableList<ObjectRepositoryEntryChanges> changes, IObjectRepository oldRepository = null)
        {
            NewRepository = newRepository ?? throw new ArgumentNullException(nameof(newRepository));
            _changes = changes ?? throw new ArgumentNullException(nameof(changes));
            OldRepository = oldRepository;

            Added = _changes.Where(c => c.Status == ChangeKind.Added).ToImmutableList();
            Modified = _changes.Where(c => c.Status == ChangeKind.Modified).ToImmutableList();
            Deleted = _changes.Where(c => c.Status == ChangeKind.Deleted).ToImmutableList();
        }

        /// <summary>
        /// Gets the old repository.
        /// </summary>
        public IObjectRepository OldRepository { get; }

        /// <summary>
        /// Gets the new repository.
        /// </summary>
        public IObjectRepository NewRepository { get; }

        /// <summary>
        /// Gets the list of <see cref="ObjectRepositoryEntryChanges" /> that have been been added.
        /// </summary>
        public IReadOnlyList<ObjectRepositoryEntryChanges> Added { get; }

        /// <summary>
        /// Gets the list of <see cref="ObjectRepositoryEntryChanges" /> that have been been modified.
        /// </summary>
        public IReadOnlyList<ObjectRepositoryEntryChanges> Modified { get; }

        /// <summary>
        /// Gets the list of <see cref="ObjectRepositoryEntryChanges" /> that have been been deleted.
        /// </summary>
        public IReadOnlyList<ObjectRepositoryEntryChanges> Deleted { get; }

        /// <inheritdoc/>
        public int Count => _changes.Count;

        /// <inheritdoc/>
        public ObjectRepositoryEntryChanges this[int index] => _changes[index];

        /// <inheritdoc/>
        public IEnumerator<ObjectRepositoryEntryChanges> GetEnumerator() => _changes.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
