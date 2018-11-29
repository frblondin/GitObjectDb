using GitObjectDb.Git;
using GitObjectDb.Models.Compare;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace GitObjectDb.Models.Rebase
{
    /// <summary>
    /// Creates a new instance implementing the <see cref="IObjectRepositoryRebase"/> interface.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="repositoryDescription">The repository description.</param>
    /// <param name="repository">The repository on which to apply the merge.</param>
    /// <param name="rebaseCommitId">The commit to be rebased.</param>
    /// <param name="branchName">Name of the branch.</param>
    /// <returns>The newly created instance.</returns>
    public delegate IObjectRepositoryRebase ObjectRepositoryRebaseFactory(IObjectRepositoryContainer container, RepositoryDescription repositoryDescription, IObjectRepository repository, ObjectId rebaseCommitId, string branchName);

    /// <summary>
    /// Encapsulates a rebase operation.
    /// </summary>
    public interface IObjectRepositoryRebase
    {
        /// <summary>
        /// Gets the head commit identifier.
        /// </summary>
        ObjectId HeadCommitId { get; }

        /// <summary>
        /// Gets the commit to be rebased.
        /// </summary>
        ObjectId RebaseCommitId { get; }

        /// <summary>
        /// Gets the name of the rebase source branch.
        /// </summary>
        string BranchName { get; }

        /// <summary>
        /// Gets the commits from the branch that are played after the last upstream
        /// branch commit.
        /// </summary>
        IImmutableList<ObjectId> ReplayedCommits { get; }

        /// <summary>
        /// Gets whether the rebase operation run until it should stop (completed the rebase,
        /// or the operation for the current step is one that sequencing should stop.
        /// </summary>
        RebaseStatus Status { get; }

        /// <summary>
        /// Gets the number of completed steps.
        /// </summary>
        int CompletedStepCount { get; }

        /// <summary>
        /// Gets the total number of steps in the rebase.
        /// </summary>
        int TotalStepCount { get; }

        /// <summary>
        /// Gets the modified chunks.
        /// </summary>
        IList<ObjectRepositoryChunkChange> ModifiedChunks { get; }

        /// <summary>
        /// Gets the added objects.
        /// </summary>
        IList<ObjectRepositoryAdd> AddedObjects { get; }

        /// <summary>
        /// Gets the deleted objects.
        /// </summary>
        IList<ObjectRepositoryDelete> DeletedObjects { get; }

#pragma warning disable CA1716 // Identifiers should not match keywords
        /// <summary>
        /// Continues the rebase operation.
        /// </summary>
        /// <returns>A current <see cref="IObjectRepositoryRebase"/> instance - used to allow chained calls.</returns>
        IObjectRepositoryRebase Continue();
#pragma warning restore CA1716 // Identifiers should not match keywords
    }
}