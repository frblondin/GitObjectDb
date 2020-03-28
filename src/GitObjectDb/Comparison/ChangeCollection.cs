using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GitObjectDb.Comparison
{
    /// <summary>Represents a collection of node changes.</summary>
    public sealed class ChangeCollection : IList<Change>
    {
        private readonly IList<Change> _changes;

        internal ChangeCollection()
        {
            _changes = new List<Change>();
        }

        /// <summary>Gets the modified.</summary>
        /// <value>The modified.</value>
        public IEnumerable<Change> Modified => _changes.Where(c => c.Status == ChangeStatus.Edit);

        /// <summary>Gets the added.</summary>
        /// <value>The added.</value>
        public IEnumerable<Change> Added => _changes.Where(c => c.Status == ChangeStatus.Add);

        /// <summary>Gets the deleted.</summary>
        /// <value>The deleted.</value>
        public IEnumerable<Change> Deleted => _changes.Where(c => c.Status == ChangeStatus.Delete);

        /// <inheritdoc/>
        public bool IsReadOnly => _changes.IsReadOnly;

        /// <inheritdoc/>
        public int Count => _changes.Count;

        /// <inheritdoc/>
        public Change this[int index]
        {
            get => _changes[index];
            set => _changes[index] = value;
        }

        /// <inheritdoc/>
        public void Add(Change item) => _changes.Add(item);

        /// <inheritdoc/>
        public void Clear() => _changes.Clear();

        /// <inheritdoc/>
        public bool Contains(Change item) => _changes.Contains(item);

        /// <inheritdoc/>
        public void CopyTo(Change[] array, int arrayIndex) => _changes.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public IEnumerator<Change> GetEnumerator() => _changes.GetEnumerator();

        /// <inheritdoc/>
        public int IndexOf(Change item) => _changes.IndexOf(item);

        /// <inheritdoc/>
        public void Insert(int index, Change item) => _changes.Insert(index, item);

        /// <inheritdoc/>
        public bool Remove(Change item) => _changes.Remove(item);

        /// <inheritdoc/>
        public void RemoveAt(int index) => _changes.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => _changes.GetEnumerator();
    }
}
