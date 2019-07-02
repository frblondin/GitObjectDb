using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using LibGit2Sharp;
using System;
using System.Collections.Generic;

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
        /// <returns>A <see cref="ObjectRepositoryChangeCollection"/> containing all computed changes.</returns>
        ObjectRepositoryChangeCollection Compare(ObjectId oldCommitId, ObjectId newCommitId);

        /// <summary>
        /// Compares two <see cref="IObjectRepository"/> instances and generates a new <see cref="TreeDefinition"/> instance containing the tree changes to be committed.
        /// </summary>
        /// <param name="original">The original.</param>
        /// <param name="newRepository">The new.</param>
        /// <returns>A <see cref="ObjectRepositoryChangeCollection"/> containing all computed changes.</returns>
        /// <exception cref="ArgumentNullException">
        /// original
        /// or
        /// new
        /// </exception>
        ObjectRepositoryChangeCollection Compare(IObjectRepository original, IObjectRepository newRepository);

        /// <summary>
        /// Computes the changes applied on the specified repository.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="modifiedProperties">The modified chunks.</param>
        /// <param name="addedObjects">The added objects.</param>
        /// <param name="deletedObjects">The deleted objects.</param>
        /// <returns>A <see cref="ObjectRepositoryChangeCollection"/> containing all computed changes.</returns>
        ObjectRepositoryChangeCollection Compute(IObjectRepository repository, IEnumerable<ObjectRepositoryPropertyChange> modifiedProperties, IList<ObjectRepositoryAdd> addedObjects, IList<ObjectRepositoryDelete> deletedObjects);
}
}