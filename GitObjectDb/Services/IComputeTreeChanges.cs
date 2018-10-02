using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using LibGit2Sharp;
using System;

namespace GitObjectDb.Services
{
    /// <summary>
    /// Creates a new instance implementing the <see cref="IComputeTreeChanges"/> interface.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="description">The description.</param>
    /// <returns>The newly created instance.</returns>
    public delegate IComputeTreeChanges ComputeTreeChangesFactory(IObjectRepositoryContainer container, RepositoryDescription description);

    /// <summary>
    /// Compares to commits and computes the differences (additions, deletions...).
    /// </summary>
    public interface IComputeTreeChanges
    {
        /// <summary>
        /// Compares the specified commits.
        /// </summary>
        /// <param name="oldCommitId">The old commit id.</param>
        /// <param name="newCommitId">The new commit id.</param>
        /// <returns>A <see cref="ObjectRepositoryChanges"/> containing all computed changes.</returns>
        ObjectRepositoryChanges Compare(ObjectId oldCommitId, ObjectId newCommitId);

        /// <summary>
        /// Compares two <see cref="AbstractObjectRepository"/> instances and generates a new <see cref="TreeDefinition"/> instance containing the tree changes to be committed.
        /// </summary>
        /// <param name="original">The original.</param>
        /// <param name="newRepository">The new.</param>
        /// <returns>The <see cref="TreeDefinition"/> and <code>true</code> is any change was detected between the two versions.</returns>
        /// <exception cref="ArgumentNullException">
        /// original
        /// or
        /// new
        /// </exception>
        ObjectRepositoryChanges Compare(IObjectRepository original, IObjectRepository newRepository);
    }
}