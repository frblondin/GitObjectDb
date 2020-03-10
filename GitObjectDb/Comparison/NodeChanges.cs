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
    public sealed class NodeChanges : IList<NodeChange>
    {
        private readonly IList<NodeChange> _changes;

        internal NodeChanges(IEnumerable<NodeChange> changes)
        {
            _changes = (changes ?? throw new ArgumentNullException(nameof(changes))).ToList();
        }

        /// <summary>Gets the modified.</summary>
        /// <value>The modified.</value>
        public IEnumerable<NodeChange> Modified => _changes.Where(c => c.Status == NodeChangeStatus.Edit);

        /// <summary>Gets the added.</summary>
        /// <value>The added.</value>
        public IEnumerable<NodeChange> Added => _changes.Where(c => c.Status == NodeChangeStatus.Add);

        /// <summary>Gets the deleted.</summary>
        /// <value>The deleted.</value>
        public IEnumerable<NodeChange> Deleted => _changes.Where(c => c.Status == NodeChangeStatus.Delete);

        /// <inheritdoc/>
        public bool IsReadOnly => _changes.IsReadOnly;

        /// <inheritdoc/>
        public int Count => _changes.Count;

        /// <inheritdoc/>
        public NodeChange this[int index] { get => _changes[index]; set => _changes[index] = value; }

        /// <inheritdoc/>
        public void Add(NodeChange item) => _changes.Add(item);

        /// <inheritdoc/>
        public void Clear() => _changes.Clear();

        /// <inheritdoc/>
        public bool Contains(NodeChange item) => _changes.Contains(item);

        /// <inheritdoc/>
        public void CopyTo(NodeChange[] array, int arrayIndex) => _changes.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public IEnumerator<NodeChange> GetEnumerator() => _changes.GetEnumerator();

        /// <inheritdoc/>
        public int IndexOf(NodeChange item) => _changes.IndexOf(item);

        /// <inheritdoc/>
        public void Insert(int index, NodeChange item) => _changes.Insert(index, item);

        /// <inheritdoc/>
        public bool Remove(NodeChange item) => _changes.Remove(item);

        /// <inheritdoc/>
        public void RemoveAt(int index) => _changes.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => _changes.GetEnumerator();
    }
}
