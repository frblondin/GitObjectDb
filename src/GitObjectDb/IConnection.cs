using GitObjectDb.Comparison;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb
{
    /// <summary>Represents a method that creates a <see cref="IConnection"/>.</summary>
    /// <param name="path">The path containing the .git repository.</param>
    /// <param name="initialBranch">Name of the default branch name.</param>
    /// <returns>A new connection instance.</returns>
    public delegate IConnection ConnectionFactory(string path, string initialBranch);

    /// <summary>Represents a GitObjectDb connection, allowing to query and perform operations on nodes.</summary>
    /// <seealso cref="IDisposable" />
    public interface IConnection : IDisposable
    {
        /// <summary>Gets high level information about this repository.</summary>
        RepositoryInformation Info { get; }

        /// <summary>Gets branches in the repository.</summary>
        BranchCollection Branches { get; }

        /// <summary>Gets the branch pointed to by HEAD.</summary>
        Branch Head { get; }

        /// <summary>Lookup and enumerate commits in the repository. Iterating this collection directly
        /// starts walking from the HEAD.</summary>
#pragma warning disable SA1623 // Property summary documentation should match accessors
        IQueryableCommitLog Commits { get; }
#pragma warning restore SA1623 // Property summary documentation should match accessors

        /// <summary>Initiates a series of node transformations.</summary>
        /// <param name="transformations">The transformations to be applied.</param>
        /// <returns>The collection of transformations.</returns>
        ITransformationComposer Update(Action<ITransformationComposer> transformations);

        /// <summary>Lookups for the item defined in the specified path.</summary>
        /// <typeparam name="TItem">The type of the node.</typeparam>
        /// <param name="path">The path.</param>
        /// <param name="committish">The committish.</param>
        /// <param name="referenceCache">Cache that can be used to reuse same shared node references between queries.</param>
        /// <returns>The item being found, if any.</returns>
        TItem Lookup<TItem>(DataPath path, string? committish = null, IDictionary<DataPath, ITreeItem>? referenceCache = null)
            where TItem : ITreeItem;

        /// <summary>Gets nodes from repository.</summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="committish">The committish.</param>
        /// <param name="isRecursive"><c>true</c> to query all nodes recursively, <c>false</c> otherwise.</param>
        /// <param name="referenceCache">Cache that can be used to reuse same shared node references between queries.</param>
        /// <returns>The <see cref="IQueryable{Node}"/> that represents the input sequence.</returns>
        IEnumerable<Node> GetNodes(Node? parent = null, string? committish = null, bool isRecursive = false, IDictionary<DataPath, ITreeItem>? referenceCache = null);

        /// <summary>Gets nodes from repository.</summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="committish">The committish.</param>
        /// <param name="isRecursive"><c>true</c> to query all nodes recursively, <c>false</c> otherwise.</param>
        /// <param name="referenceCache">Cache that can be used to reuse same shared node references between queries.</param>
        /// <typeparam name="TResult">The type of requested nodes.</typeparam>
        /// <returns>The <see cref="IQueryable{Node}"/> that represents the input sequence.</returns>
        IEnumerable<TResult> GetNodes<TResult>(Node? parent = null, string? committish = null, bool isRecursive = false, IDictionary<DataPath, ITreeItem>? referenceCache = null)
            where TResult : Node;

        /// <summary>Gets data paths from repository.</summary>
        /// <param name="parentPath">The parent node path.</param>
        /// <param name="committish">The committish.</param>
        /// <param name="isRecursive"><c>true</c> to query all nodes recursively, <c>false</c> otherwise.</param>
        /// <returns>The <see cref="IQueryable{Node}"/> that represents the input sequence.</returns>
        IEnumerable<DataPath> GetPaths(DataPath? parentPath = null, string? committish = null, bool isRecursive = false);

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
        /// <returns>All nested resources.</returns>
        public IEnumerable<Resource> GetResources(Node node, string? committish = null);

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
        Repository Repository { get; }

        Commit FindUpstreamCommit(string? committish, Branch branch);
    }
}
