using GitObjectDb.Compare;
using GitObjectDb.Git;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Root node of a metadata tree.
    /// </summary>
    /// <seealso cref="IMetadataObject" />
    public interface IObjectRepository : IMetadataObject
    {
        /// <summary>
        /// Gets the <see cref="LibGit2Sharp.Commit"/> id this instance has been load from.
        /// </summary>
        ObjectId CommitId { get; }

        /// <summary>
        /// Stores the content of this instance and all its children in a new Git repository.
        /// </summary>
        /// <param name="signature">The signature.</param>
        /// <param name="message">The message.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="isBare">if set to <c>true</c> a bare Git repository will be created.</param>
        /// <returns>The commit identifier of the new repository HEAD.</returns>
        ObjectId SaveInNewRepository(Signature signature, string message, RepositoryDescription repositoryDescription, bool isBare = false);

        /// <summary>
        /// Commits all changes by comparing the current instance with a new one.
        /// </summary>
        /// <param name="newRepository">The new repository containing all the changes that must be committed.</param>
        /// <param name="signature">The signature.</param>
        /// <param name="message">The message.</param>
        /// <param name="options">The options.</param>
        /// <returns>The commit identifier of the new repository HEAD.</returns>
        ObjectId Commit(AbstractObjectRepository newRepository, Signature signature, string message, CommitOptions options = null);

        /// <summary>
        /// Checkouts the specified branch name.
        /// </summary>
        /// <param name="branchName">Name of the branch.</param>
        /// <returns>The <see cref="LibGit2Sharp.Branch"/>.</returns>
        Branch Checkout(string branchName);

        /// <summary>
        /// Creates a branch with the specified name. This branch will point at the current commit.
        /// </summary>
        /// <param name="branchName">The name of the branch to create.</param>
        /// <returns>The newly created <see cref="LibGit2Sharp.Branch"/>.</returns>
        Branch Branch(string branchName);

        /// <summary>
        /// Merges changes from branch into the branch pointed at by HEAD..
        /// </summary>
        /// <param name="branchName">Name of the branch.</param>
        /// <returns>The <see cref="IMetadataTreeMerge"/> instance used to apply the merge.</returns>
        IMetadataTreeMerge Merge(string branchName);

        /// <summary>
        /// Tries getting a nested object from its Git path.
        /// </summary>
        /// <param name="path">The Git path.</param>
        /// <returns>The object, if any was found.</returns>
        IMetadataObject TryGetFromGitPath(string path);

        /// <summary>
        /// Gets a nested object from its Git path.
        /// </summary>
        /// <param name="path">The Git path.</param>
        /// <returns>The object, if any was found.</returns>
        IMetadataObject GetFromGitPath(string path);
    }
}
