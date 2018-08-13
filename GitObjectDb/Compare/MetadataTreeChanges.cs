using GitObjectDb.Models;
using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GitObjectDb.Compare
{
    /// <summary>
    /// Holds the result of a diff between two trees.
    /// </summary>
    [DebuggerDisplay("+{Added.Count} ~{Modified.Count} -{Deleted.Count}")]
    public class MetadataTreeChanges : IReadOnlyList<MetadataTreeEntryChanges>
    {
        readonly IImmutableList<MetadataTreeEntryChanges> _changes;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataTreeChanges"/> class.
        /// </summary>
        /// <param name="newRepository">The new repository.</param>
        /// <param name="changed">The list of <see cref="MetadataTreeEntryChanges" /> that have been been changed.</param>
        /// <param name="oldRepository">The old repository.</param>
        /// <exception cref="ArgumentNullException">
        /// modified
        /// or
        /// added
        /// or
        /// deleted
        /// </exception>
        public MetadataTreeChanges(IObjectRepository newRepository, IImmutableList<MetadataTreeEntryChanges> changed, IObjectRepository oldRepository = null)
        {
            NewRepository = newRepository ?? throw new ArgumentNullException(nameof(newRepository));
            _changes = changed ?? throw new ArgumentNullException(nameof(changed));
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
        /// Gets the list of <see cref="MetadataTreeEntryChanges" /> that have been been added.
        /// </summary>
        public IReadOnlyList<MetadataTreeEntryChanges> Added { get; }

        /// <summary>
        /// Gets the list of <see cref="MetadataTreeEntryChanges" /> that have been been modified.
        /// </summary>
        public IReadOnlyList<MetadataTreeEntryChanges> Modified { get; }

        /// <summary>
        /// Gets the list of <see cref="MetadataTreeEntryChanges" /> that have been been deleted.
        /// </summary>
        public IReadOnlyList<MetadataTreeEntryChanges> Deleted { get; }

        /// <inheritdoc/>
        public int Count => _changes.Count;

        /// <inheritdoc/>
        public MetadataTreeEntryChanges this[int index] => _changes[index];

        /// <inheritdoc/>
        public IEnumerator<MetadataTreeEntryChanges> GetEnumerator() => _changes.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Updates the tree definition.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="definition">The definition.</param>
        internal void UpdateTreeDefinition(IRepository repository, TreeDefinition definition)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var buffer = new StringBuilder();
            foreach (var change in Modified.Concat(Added))
            {
                change.New.ToJson(buffer);
                definition.Add(change.Path, repository.CreateBlob(buffer), Mode.NonExecutableFile);
            }
            foreach (var deleted in Deleted)
            {
                definition.Remove(deleted.Path);
            }
        }
    }
}
