using GitObjectDb.Comparison;
using GitObjectDb.Model;
using LibGit2Sharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb
{
    /// <summary>Represents a method that creates a <see cref="IConnection"/>.</summary>
    /// <param name="path">The path containing the .git repository.</param>
    /// <param name="model">Model that this connection should manage.</param>
    /// <param name="initialBranch">Name of the default branch name.</param>
    /// <returns>A new connection instance.</returns>
    public delegate IConnection ConnectionFactory(string path, IDataModel model, string initialBranch = "main");

    /// <summary>Represents a GitObjectDb connection, allowing to query and perform operations on nodes.</summary>
    /// <seealso cref="IDisposable" />
    public interface IConnection : IDisposable
    {
        /// <summary>Gets the underlying Git repository.</summary>
        IRepository Repository { get; }

        /// <summary>Gets high level information about this repository.</summary>
        RepositoryInformation Info { get; }

        /// <summary>Gets branches in the repository.</summary>
        BranchCollection Branches { get; }

        /// <summary>Gets the branch pointed to by HEAD.</summary>
        Branch Head { get; }

        /// <summary>Lookups and enumerates commits in the repository. Iterating this collection directly
        /// starts walking from the HEAD.</summary>
#pragma warning disable SA1623 // Property summary documentation should match accessors
        IQueryableCommitLog Commits { get; }
#pragma warning restore SA1623 // Property summary documentation should match accessors

        /// <summary>Gets the model that this connection should manage.</summary>
        IDataModel Model { get; }

        /// <summary>Initiates a series of node transformations.</summary>
        /// <param name="transformations">The transformations to be applied.</param>
        /// <returns>The collection of transformations.</returns>
        ITransformationComposer Update(Action<ITransformationComposer>? transformations = null);

        /// <summary>Lookups for the item defined in the specified path.</summary>
        /// <typeparam name="TItem">The type of the node.</typeparam>
        /// <param name="path">The path.</param>
        /// <param name="committish">The committish.</param>
        /// <param name="referenceCache">Cache that can be used to reuse same shared node references between queries.</param>
        /// <returns>The item being found, if any.</returns>
        TItem Lookup<TItem>(DataPath path, string? committish = null, ConcurrentDictionary<DataPath, ITreeItem>? referenceCache = null)
            where TItem : ITreeItem;

        /// <summary>Gets all items from repository.</summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="committish">The committish.</param>
        /// <param name="isRecursive"><c>true</c> to query all nodes recursively, <c>false</c> otherwise.</param>
        /// <param name="referenceCache">Cache that can be used to reuse same shared node references between queries.</param>
        /// <typeparam name="TItem">The type of requested items.</typeparam>
        /// <returns>The <see cref="IQueryable{Node}"/> that represents the input sequence.</returns>
        IEnumerable<TItem> GetItems<TItem>(Node? parent = null, string? committish = null, bool isRecursive = false, ConcurrentDictionary<DataPath, ITreeItem>? referenceCache = null)
            where TItem : ITreeItem;

        /// <summary>Gets nodes from repository.</summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="committish">The committish.</param>
        /// <param name="isRecursive"><c>true</c> to query all nodes recursively, <c>false</c> otherwise.</param>
        /// <param name="referenceCache">Cache that can be used to reuse same shared node references between queries.</param>
        /// <typeparam name="TNode">The type of requested nodes.</typeparam>
        /// <returns>The <see cref="IEnumerable{TNode}"/> that represents the input sequence.</returns>
        IEnumerable<TNode> GetNodes<TNode>(Node? parent = null, string? committish = null, bool isRecursive = false, ConcurrentDictionary<DataPath, ITreeItem>? referenceCache = null)
            where TNode : Node;

        /// <summary>Gets data paths from repository.</summary>
        /// <param name="parentPath">The parent node path.</param>
        /// <param name="committish">The committish.</param>
        /// <param name="isRecursive"><c>true</c> to query all nodes recursively, <c>false</c> otherwise.</param>
        /// <returns>The <see cref="IEnumerable{Node}"/> that represents the input sequence.</returns>
        IEnumerable<DataPath> GetPaths(DataPath? parentPath = null, string? committish = null, bool isRecursive = false);

        /// <summary>Gets data paths from repository.</summary>
        /// <param name="parentPath">The parent node path.</param>
        /// <param name="committish">The committish.</param>
        /// <param name="isRecursive"><c>true</c> to query all nodes recursively, <c>false</c> otherwise.</param>
        /// <typeparam name="TItem">The type of requested item paths nodes.</typeparam>
        /// <returns>The <see cref="IEnumerable{Node}"/> that represents the input sequence.</returns>
        IEnumerable<DataPath> GetPaths<TItem>(DataPath? parentPath = null, string? committish = null, bool isRecursive = false)
            where TItem : ITreeItem;

        /// <summary>Compares two commits (additions, deletions, editions, conflicts).</summary>
        /// <param name="startCommittish">Starting points of comparison.</param>
        /// <param name="committish">End point of comparison.</param>
        /// <param name="policy">The merge policy to use.</param>
        /// <returns>Details about the comparison.</returns>
        ChangeCollection Compare(string startCommittish, string? committish = null, ComparisonPolicy? policy = null);

        /// <summary>
        /// Gets the resources associated to the node.
        /// </summary>
        /// <param name="node">The parent node.</param>
        /// <param name="committish">The committish.</param>
        /// <param name="referenceCache">Cache that can be used to reuse same shared node references between queries.</param>
        /// <returns>All nested resources.</returns>
        public IEnumerable<Resource> GetResources(Node node, string? committish = null, ConcurrentDictionary<DataPath, ITreeItem>? referenceCache = null);

        /// <summary>Checkouts the specified branch name.</summary>
        /// <param name="branchName">Name of the branch.</param>
        /// <param name="committish">The committish.</param>
        /// <returns>The branch being checked out.</returns>
        Branch Checkout(string branchName, string? committish = null);

        /// <summary>Rebases changes from upstream into the branch.</summary>
        /// <param name="branch">The branch to merge changes into.</param>
        /// <param name="upstreamCommittish">The upstream committish.</param>
        /// <param name="policy">The merge policy.</param>
        /// <returns>The resut of the rebase operation.</returns>
        IRebase Rebase(Branch? branch = null, string? upstreamCommittish = null, ComparisonPolicy? policy = null);

        /// <summary>Merges changes from upstream into the branch.</summary>
        /// <param name="branch">The branch to merge changes into.</param>
        /// <param name="upstreamCommittish">The upstream committish.</param>
        /// <param name="policy">The merge policy.</param>
        /// <returns>The resut of the rebase operation.</returns>
        IMerge Merge(Branch? branch = null, string? upstreamCommittish = null, ComparisonPolicy? policy = null);

        /// <summary>
        /// Cherry-picks the specified commit.
        /// </summary>
        /// <param name="committish">The commit to cherry-pick.</param>
        /// <param name="committer">The <see cref="Signature"/> of who is performing the cherry pick.</param>
        /// <param name="branch">The branch to cherry-pick into.</param>
        /// <param name="policy">The cherry-pick policy.</param>
        /// <returns>The result of the cherry-pick operation.</returns>
        ICherryPick CherryPick(string committish, Signature? committer = null, Branch? branch = null, CherryPickPolicy? policy = null);
    }

    internal interface IConnectionInternal : IConnection
    {
        Commit FindUpstreamCommit(string? committish, Branch branch);
    }
}
