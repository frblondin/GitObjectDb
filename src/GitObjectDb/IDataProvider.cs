using GitDotNet;
using GitObjectDb.Model;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb;

/// <summary>Provides various queries to access GitObjectDb items.</summary>
public interface IDataProvider
{
    /// <summary>Gets the model that this connection should manage.</summary>
    IDataModel Model { get; }

    /// <summary>Gets the node serializer.</summary>
    INodeSerializer Serializer { get; }

    /// <summary>Gets the cache that can be used to reuse same shared node references between queries.</summary>
    IMemoryCache Cache { get; }

    /// <summary>Asynchronously retrieves an item from a specified path in a commit entry.</summary>
    /// <typeparam name="TItem">Represents the type of item being retrieved from the data structure.</typeparam>
    /// <param name="commit">Specifies the commit entry from which to look up the item.</param>
    /// <param name="path">Indicates the location in the data structure to search for the item.</param>
    /// <returns>Returns the found item or null if it does not exist.</returns>
    Task<TItem?> LookupAsync<TItem>(CommitEntry commit, DataPath path)
        where TItem : TreeItem;

    /// <summary>
    /// Asynchronously retrieves an item based on a unique identifier and a specific commit entry.
    /// If two nodes have the same id in two different hierarchy trees, the first matching node will be returned.
    /// </summary>
    /// <typeparam name="TItem">Represents the type of the item being retrieved, constrained to a specific base type.</typeparam>
    /// <param name="commit">Specifies the commit entry from which the item should be looked up.</param>
    /// <param name="id">Indicates the unique identifier of the item to be retrieved.</param>
    /// <returns>Returns the retrieved item or null if not found.</returns>
    Task<TItem?> LookupAsync<TItem>(CommitEntry commit, UniqueId id)
        where TItem : TreeItem;

    /// <summary>
    /// Retrieves an asynchronous enumerable collection of items based on the specified commit and optional parameters.
    /// </summary>
    /// <typeparam name="TItem">Represents the type of items being retrieved, constrained to a specific base type.</typeparam>
    /// <param name="commit">Specifies the commit from which to retrieve the items.</param>
    /// <param name="parent">Indicates an optional parent node to filter the items being retrieved.</param>
    /// <param name="isRecursive">Determines whether to include items from child nodes in the retrieval process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerable collection of items matching the specified criteria.</returns>
    IAsyncEnumerable<TItem> GetItemsAsync<TItem>(CommitEntry commit,
        Node? parent = null,
        bool isRecursive = false,
        CancellationToken cancellationToken = default)
        where TItem : TreeItem;

    /// <summary>
    /// Retrieves an asynchronous collection of nodes based on the specified commit and optional parent node.
    /// </summary>
    /// <typeparam name="TNode">Represents a type of node that can be retrieved from the commit.</typeparam>
    /// <param name="commit">Specifies the commit entry from which to retrieve the nodes.</param>
    /// <param name="parent">Indicates an optional parent node to filter the retrieved nodes.</param>
    /// <param name="isRecursive">Determines whether to include child nodes in the retrieval process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerable collection of nodes matching the criteria.</returns>
    IAsyncEnumerable<TNode> GetNodesAsync<TNode>(CommitEntry commit,
        Node? parent = null,
        bool isRecursive = false,
        CancellationToken cancellationToken = default)
        where TNode : Node;

    /// <summary>
    /// Retrieves a collection of data paths asynchronously based on a specified commit. It can filter paths by a parent
    /// path and support recursive retrieval.
    /// </summary>
    /// <param name="commit">Specifies the commit entry to use for fetching the data paths.</param>
    /// <param name="parentPath">Defines an optional starting point for the paths to be retrieved.</param>
    /// <param name="isRecursive">Indicates whether to include paths from subdirectories in the retrieval.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerable collection of data paths.</returns>
    IAsyncEnumerable<DataPath> GetPathsAsync(CommitEntry commit,
        DataPath? parentPath = null,
        bool isRecursive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an asynchronous enumerable collection of data paths based on the specified commit and optional
    /// parameters.
    /// </summary>
    /// <typeparam name="TItem">Represents a type that extends a tree item, allowing for specific item handling in the path retrieval.</typeparam>
    /// <param name="commit">Specifies the commit entry from which to retrieve the data paths.</param>
    /// <param name="parentPath">Defines an optional starting point for the path retrieval, allowing for targeted searches.</param>
    /// <param name="isRecursive">Indicates whether to include paths from child items in the retrieval process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerable collection of data paths that match the specified criteria.</returns>
    IAsyncEnumerable<DataPath> GetPathsAsync<TItem>(CommitEntry commit,
        DataPath? parentPath = null,
        bool isRecursive = false,
        CancellationToken cancellationToken = default)
        where TItem : TreeItem;

    /// <summary>
    /// Searches for tree items asynchronously based on a specified commit and pattern.
    /// </summary>
    /// <param name="commit">Specifies the commit from which to search for tree items.</param>
    /// <param name="pattern">Defines the search criteria for matching tree items.</param>
    /// <param name="parentPath">Indicates the directory path to limit the search scope.</param>
    /// <param name="ignoreCase">Determines whether the search should be case-sensitive or not.</param>
    /// <param name="recurseSubModules">Indicates if the search should include submodules.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerable collection of matching tree items.</returns>
    IAsyncEnumerable<TreeItem> SearchAsync(CommitEntry commit,
        string pattern,
        DataPath? parentPath = null,
        bool ignoreCase = false,
        bool recurseSubModules = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a collection of resources asynchronously based on a specific commit and node.
    /// </summary>
    /// <param name="commit">Represents a specific point in the version history to fetch resources from.</param>
    /// <param name="node">Indicates the specific location or context within the resource structure.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerable collection of resources related to the provided commit and node.</returns>
    IAsyncEnumerable<Resource> GetResourcesAsync(CommitEntry commit, Node node, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a collection of commit entries asynchronously based on a specific commit and tree item.
    /// </summary>
    /// <param name="commit">Specifies the commit from which to retrieve related commit entries.</param>
    /// <param name="item">Indicates the tree item associated with the commits being retrieved.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerable collection of commit entries.</returns>
    IAsyncEnumerable<LogEntry> GetLogsAsync(CommitEntry commit, TreeItem item, CancellationToken cancellationToken = default);
}