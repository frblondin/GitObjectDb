using GitObjectDb.Model;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace GitObjectDb;

/// <summary>Provides various queries to access GitObjectDb items.</summary>
public interface IQueryAccessor
{
    /// <summary>Gets the model that this connection should manage.</summary>
    IDataModel Model { get; }

    /// <summary>Gets the node serializer.</summary>
    INodeSerializer Serializer { get; }

    /// <summary>Gets the cache that can be used to reuse same shared node references between queries.</summary>
    IMemoryCache? Cache { get; }

    /// <summary>Lookups for the item defined in the specified path.</summary>
    /// <typeparam name="TItem">The type of the node.</typeparam>
    /// <param name="path">The path.</param>
    /// <param name="committish">The optional committish (head tip is used otherwise).</param>
    /// <returns>The item being found, if any.</returns>
    TItem? Lookup<TItem>(DataPath path,
                        string? committish = null)
        where TItem : ITreeItem;

    /// <summary>Lookups for the item defined by its unique identifier.</summary>
    /// <typeparam name="TItem">The type of the node.</typeparam>
    /// <param name="id">The unique identifier.</param>
    /// <param name="committish">The optional committish (head tip is used otherwise).</param>
    /// <returns>The item being found, if any.</returns>
    TItem? Lookup<TItem>(UniqueId id,
                           string? committish = null)
        where TItem : ITreeItem;

    /// <summary>Gets all items from repository.</summary>
    /// <param name="parent">The parent node.</param>
    /// <param name="committish">The optional committish (head tip is used otherwise).</param>
    /// <param name="isRecursive"><c>true</c> to query all nodes recursively, <c>false</c> otherwise.</param>
    /// <typeparam name="TItem">The type of requested items.</typeparam>
    /// <returns>The items being found, if any.</returns>
    ICommitEnumerable<TItem> GetItems<TItem>(Node? parent = null,
                                             string? committish = null,
                                             bool isRecursive = false)
        where TItem : ITreeItem;

    /// <summary>Gets nodes from repository.</summary>
    /// <param name="parent">The parent node.</param>
    /// <param name="committish">The committish.</param>
    /// <param name="isRecursive"><c>true</c> to query all nodes recursively, <c>false</c> otherwise.</param>
    /// <typeparam name="TNode">The type of requested nodes.</typeparam>
    /// <returns>The items being found, if any.</returns>
    ICommitEnumerable<TNode> GetNodes<TNode>(Node? parent = null,
                                             string? committish = null,
                                             bool isRecursive = false)
        where TNode : Node;

    /// <summary>Gets data paths from repository.</summary>
    /// <param name="parentPath">The parent node path.</param>
    /// <param name="committish">The optional committish (head tip is used otherwise).</param>
    /// <param name="isRecursive"><c>true</c> to query all nodes recursively, <c>false</c> otherwise.</param>
    /// <returns>The paths being found, if any.</returns>
    IEnumerable<DataPath> GetPaths(DataPath? parentPath = null,
                                   string? committish = null,
                                   bool isRecursive = false);

    /// <summary>Gets data paths from repository.</summary>
    /// <param name="parentPath">The parent node path.</param>
    /// <param name="committish">The optional committish (head tip is used otherwise).</param>
    /// <param name="isRecursive"><c>true</c> to query all nodes recursively, <c>false</c> otherwise.</param>
    /// <typeparam name="TItem">The type of requested item paths nodes.</typeparam>
    /// <returns>The paths being found, if any.</returns>
    IEnumerable<DataPath> GetPaths<TItem>(DataPath? parentPath = null,
                                          string? committish = null,
                                          bool isRecursive = false)
        where TItem : ITreeItem;

    /// <summary>Looks for specified pattern from repository.</summary>
    /// <param name="pattern">The search expression.</param>
    /// <param name="parentPath">The parent node path.</param>
    /// <param name="committish">The optional committish (head tip is used otherwise).</param>
    /// <param name="ignoreCase">Ignore case differences between the patterns and the files.</param>
    /// <param name="recurseSubModules">Recursively search in each submodule that is active and checked out in the repository.</param>
    /// <returns>The items being found, if any.</returns>
    public IEnumerable<ITreeItem> Search(string pattern,
                                         DataPath? parentPath = null,
                                         string? committish = null,
                                         bool ignoreCase = false,
                                         bool recurseSubModules = false);

    /// <summary>Gets the resources associated to the node.</summary>
    /// <param name="node">The parent node.</param>
    /// <param name="committish">The optional committish (head tip is used otherwise).</param>
    /// <returns>All nested resources.</returns>
    public ICommitEnumerable<Resource> GetResources(Node node,
                                                    string? committish = null);

    /// <summary>Gets the history of a node.</summary>
    /// <param name="node">The node whose commits should be returned.</param>
    /// <param name="branch">The branch to get log from.</param>
    /// <returns>The node history.</returns>
    public IEnumerable<LogEntry> GetCommits(Node node, string? branch = null);
}