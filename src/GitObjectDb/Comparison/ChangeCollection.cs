using LibGit2Sharp;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GitObjectDb.Comparison;

/// <summary>Represents a collection of node changes.</summary>
public sealed class ChangeCollection : IList<Change>
{
    private readonly IList<Change> _changes;

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
    [ExcludeFromCodeCoverage]
    public bool IsReadOnly => _changes.IsReadOnly;

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
}
