using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GitObjectDb.Comparison;

/// <summary>Represents a collection of node changes.</summary>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class ChangeCollection : IList<Change>
{
    private readonly List<Change> _changes;

    internal ChangeCollection(Commit start, Commit end)
    {
        _changes = new List<Change>();
        Start = start;
        End = end;
    }

    /// <summary>Gets the first commit of changes.</summary>
    public Commit Start { get; }

    /// <summary>Gets the last commit of changes.</summary>
    public Commit End { get; }

    /// <summary>Gets the modified items.</summary>
    public IEnumerable<Change> Modified => _changes.Where(c => c.Status == ChangeStatus.Edit);

    /// <summary>Gets the added items.</summary>
    public IEnumerable<Change> Added => _changes.Where(c => c.Status == ChangeStatus.Add);

    /// <summary>Gets the deleted items.</summary>
    public IEnumerable<Change> Deleted => _changes.Where(c => c.Status == ChangeStatus.Delete);

    /// <summary>Gets the renamed items.</summary>
    public IEnumerable<Change> Renamed => _changes.Where(c => c.Status == ChangeStatus.Rename);

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public bool IsReadOnly => ((IList<Change>)_changes).IsReadOnly;

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public int Count => _changes.Count;

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public Change this[int index]
    {
        get => _changes[index];
        set => _changes[index] = value;
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public void Add(Change item) => _changes.Add(item);

    /// <summary>Adds the elements of the specified collection to the end of the changes.</summary>
    /// <param name="collection">The collection whose elements should be added.</param>
    [ExcludeFromCodeCoverage]
    public void AddRange(IEnumerable<Change> collection) => _changes.AddRange(collection);

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public void Clear() => _changes.Clear();

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public bool Contains(Change item) => _changes.Contains(item);

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public void CopyTo(Change[] array, int arrayIndex) => _changes.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public IEnumerator<Change> GetEnumerator() => _changes.GetEnumerator();

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public int IndexOf(Change item) => _changes.IndexOf(item);

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public void Insert(int index, Change item) => _changes.Insert(index, item);

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public bool Remove(Change item) => _changes.Remove(item);

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public void RemoveAt(int index) => _changes.RemoveAt(index);

    [ExcludeFromCodeCoverage]
    IEnumerator IEnumerable.GetEnumerator() => _changes.GetEnumerator();

    internal sealed class DebugView
    {
        private readonly ChangeCollection _collection;

        public DebugView(ChangeCollection collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Change[] Items
        {
            get
            {
                var array = new Change[_collection.Count];
                _collection.CopyTo(array, 0);
                return array;
            }
        }
    }
}
