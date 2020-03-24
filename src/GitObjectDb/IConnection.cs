using GitObjectDb.Comparison;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb
{
    /// <summary>Represents a method that creates a <see cref="IConnection"/>.</summary>
    /// <param name="path">The path containing the .git repository.</param>
    /// <returns>A new connection instance.</returns>
    public delegate IConnection ConnectionFactory(string path);

    /// <summary>Represents a GitObjectDb connection, allowing to query and perform operations on nodes.</summary>
    /// <seealso cref="IDisposable" />
    public interface IConnection : IDisposable
    {
        /// <summary>Gets branches in the repository.</summary>
        BranchCollection Branches { get; }

        /// <summary>Gets the branch pointed to by HEAD.</summary>
        Branch Head { get; }

        /// <summary>Initiates a series of node transformations.</summary>
        /// <param name="transformations">The transformations to be applied.</param>
        /// <returns>The collection of transformations.</returns>
        INodeTransformationComposer Update(Func<INodeTransformationComposer, INodeTransformationComposer> transformations);

        /// <summary>Lookups for the item defined in the specified path.</summary>
        /// <typeparam name="TItem">The type of the node.</typeparam>
        /// <param name="path">The path.</param>
        /// <param name="committish">The committish.</param>
        /// <returns>The item being found, if any.</returns>
        TItem Lookup<TItem>(DataPath path, string? committish = null)
            where TItem : ITreeItem;

        /// <summary>Gets a queryable object to perform search operations on the repository.</summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="committish">The committish.</param>
        /// <param name="isRecursive"><c>true</c> to query all nodes recursively, <c>false</c> otherwise.</param>
        /// <returns>The <see cref="IQueryable{Node}"/> that represents the input sequence.</returns>
        IQueryable<Node> AsQueryable(Node? parent = null, string? committish = null, bool isRecursive = false);

        /// <summary>
        /// Gets the resources associated to the node.
        /// </summary>
        /// <param name="node">The parent node.</param>
        /// <param name="committish">The committish.</param>
        /// <returns>All nested resources.</returns>
        public IEnumerable<Resource> GetResources(Node node, string? committish = null);

        /// <summary>Checkouts the specified branch name.</summary>
        /// <param name="branchName">Name of the branch.</param>
        /// <param name="createNewBranch">If set to <c>true</c>, create new branch.</param>
        /// <param name="committish">The committish.</param>
        /// <returns>The branch being checked out.</returns>
        Branch Checkout(string branchName, bool createNewBranch = false, string? committish = null);

        /// <summary>Try to lookup an object by its sha or a reference name.</summary>
        /// <typeparam name="T">The kind of <see cref="GitObject"/> to lookup.</typeparam>
        /// <param name="objectish">The revparse spec for the object to lookup.</param>
        /// <returns>The retrieved <see cref="GitObject"/>, or null if none was found.</returns>
        T Lookup<T>(string objectish)
            where T : GitObject;

        /// <summary>Rebases changes from upstream into the branch.</summary>
        /// <param name="branch">The branch to merge changes into.</param>
        /// <param name="upstreamCommittish">The upstream committish.</param>
        /// <param name="policy">The merge policy.</param>
        /// <returns>The resut of the rebase operation.</returns>
        INodeRebase Rebase(Branch? branch = null, string? upstreamCommittish = null, ComparisonPolicy? policy = null);

        /// <summary>Merges changes from upstream into the branch.</summary>
        /// <param name="branch">The branch to merge changes into.</param>
        /// <param name="upstreamCommittish">The upstream committish.</param>
        /// <param name="policy">The merge policy.</param>
        /// <returns>The resut of the rebase operation.</returns>
        INodeMerge Merge(Branch? branch = null, string? upstreamCommittish = null, ComparisonPolicy? policy = null);
    }

    internal interface IConnectionInternal : IConnection
    {
        Repository Repository { get; }
    }
}
