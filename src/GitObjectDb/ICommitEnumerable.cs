using LibGit2Sharp;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb;

/// <summary>
/// Exposes the enumerator, which supports a simple iteration over a collection of a specified type.
/// </summary>
/// <typeparam name="TItem">The type of items to enumerate.</typeparam>
public interface ICommitEnumerable<out TItem> : IEnumerable<TItem>
    where TItem : TreeItem
{
    /// <summary>Gets the commit id containing the nodes.</summary>
    ObjectId CommitId { get; }
}

#pragma warning disable SA1402 // File may only contain a single type
internal class CommitEnumerable<TItem> : ICommitEnumerable<TItem>
    where TItem : TreeItem
{
    private readonly IEnumerable<TItem> _items;

    internal CommitEnumerable(IEnumerable<TItem> items, ObjectId commitId)
    {
        _items = items;
        CommitId = commitId;
    }

    public ObjectId CommitId { get; }

    public IEnumerator<TItem> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_items).GetEnumerator();
}

/// <summary>
/// Provides a set of static methods for querying objects that implement <see cref="ICommitEnumerable{TItem}"/>.
/// </summary>
public static class CommitEnumerable
{
    /// <summary>
    /// Converts an <see cref="IEnumerable{T}"/> to an <see cref="ICommitEnumerable{TItem}"/>.
    /// </summary>
    /// <typeparam name="TItem">Type of items.</typeparam>
    /// <param name="items">The enumerable to be converted.</param>
    /// <param name="commitId">Commit used to gather the items.</param>
    /// <returns>The converted enumerable.</returns>
    public static ICommitEnumerable<TItem> ToCommitEnumerable<TItem>(this IEnumerable<TItem> items,
                                                                       ObjectId commitId)
       where TItem : TreeItem =>
       new CommitEnumerable<TItem>(items, commitId);

    /// <summary>
    /// Returns an empty <see cref="ICommitEnumerable{TItem}"/> that has the specified type argument.
    /// </summary>
    /// <typeparam name="TItem">Type of items.</typeparam>
    /// <param name="commitId">Commit used to gather the items.</param>
    /// <returns>An empty <see cref="ICommitEnumerable{TItem}"/> whose type argument is <typeparamref name="TItem"/>.</returns>
    public static ICommitEnumerable<TItem> Empty<TItem>(ObjectId commitId)
        where TItem : TreeItem =>
        Enumerable.Empty<TItem>().ToCommitEnumerable(commitId);
}