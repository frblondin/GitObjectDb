using LibGit2Sharp;
using Realms;
using System;
using System.Collections.Generic;
using System.IO;

namespace GitObjectDb;

/// <summary>
/// The staging area to prepare and aggregate the changes that will be part of the next commit.
/// </summary>
public interface IIndex : ITransformationComposer, IEnumerable<IndexEntry>
{
    /// <summary>Gets index target commit id.</summary>
    LibGit2Sharp.ObjectId? CommitId { get; }

    /// <summary>Gets the current unique version of index.</summary>
    public Guid? Version { get; }

    /// <summary>Gets the number of entries in the index.</summary>
    public int Count { get; }

    /// <summary>Gets an <see cref="IndexEntry"/> from index from its path, if any.</summary>
    /// <param name="path">The path of entry.</param>
    /// <returns>The existing entry from index.</returns>
    /// <exception cref="KeyNotFoundException">Path could not be found.</exception>
    IndexEntry this[DataPath path] { get; }

    /// <summary>Deletes all entries from index.</summary>
    void Reset();

    /// <summary>
    /// The index automatically ensures that the branch tip remains the same between the creation of the index
    /// until it gets committed. This method retarget the index to the new branch tip.
    /// </summary>
    void UpdateToBranchTip();

    /// <summary>Applies the transformation and store them in a new commit.</summary>
    /// <param name="description">The commit description.</param>
    /// <returns>The resulting commit.</returns>
    Commit Commit(CommitDescription description);

    /// <summary>Gets an <see cref="IndexEntry"/> from index from its path, if any.</summary>
    /// <param name="path">The path of entry.</param>
    /// <returns>The existing entry from index.</returns>
    IndexEntry? TryLoadEntry(DataPath path);

    /// <summary>Gets an <see cref="TreeItem"/> from index from its path, if any.</summary>
    /// <typeparam name="TItem">The type of <see cref="TreeItem"/>.</typeparam>
    /// <param name="path">The path of entry.</param>
    /// <param name="onlyIndex">Sets whether only index entries should be requested.</param>
    /// <returns>The item from index.</returns>
    TItem? TryLoadItem<TItem>(DataPath path, bool onlyIndex = false)
        where TItem : TreeItem;

    /// <summary>Gets an <see cref="TreeItem"/> from index from its path, if any.</summary>
    /// <param name="entry">The entry.</param>
    /// <returns>The item from index.</returns>
    TreeItem LoadItem(IndexEntry entry);
}

/// <summary>Description of an entry in an index.</summary>
public partial class IndexEntry : IRealmObject
{
    /// <summary>Gets the <see cref="DataPath"/> of the entry.</summary>
    public DataPath? Path => DataPath.TryParse(PathAsString, out var result) ? result : null;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    [PrimaryKey]
    [Indexed]
    internal string PathAsString { get; set; }
#pragma warning restore CS8618

    /// <summary>Gets a value indicating whether the item gets deleted.</summary>
    public bool Delete { get; internal set; }

    /// <summary>Gets the url of remote resource repository.</summary>
    public string? RemoteResourceRepository { get; internal set; }

    /// <summary>Gets the branch in remote resource repository.</summary>
    public string? RemoteResourceSha { get; internal set; }

    /// <summary>Gets entry content, if any.</summary>
    public byte[]? Data { get; internal set; }
}