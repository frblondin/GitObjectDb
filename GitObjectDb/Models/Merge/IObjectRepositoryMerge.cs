using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Reflection;
using GitObjectDb.Services;
using LibGit2Sharp;
using System.Collections.Generic;

namespace GitObjectDb.Models.Merge
{
    /// <summary>
    /// Creates a new instance implementing the <see cref="IObjectRepositoryMerge"/> interface.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="repositoryDescription">The repository description.</param>
    /// <param name="repository">The repository on which to apply the merge.</param>
    /// <param name="mergeCommitId">The commit to be merged.</param>
    /// <param name="branchName">Name of the branch.</param>
    /// <returns>The newly created instance.</returns>
    public delegate IObjectRepositoryMerge ObjectRepositoryMergeFactory(IObjectRepositoryContainer container, RepositoryDescription repositoryDescription, IObjectRepository repository, ObjectId mergeCommitId, string branchName);

    /// <summary>
    /// Provides the ability to merge changes between two branches.
    /// </summary>
    public interface IObjectRepositoryMerge
    {
        /// <summary>
        /// Gets the head commit identifier.
        /// </summary>
        ObjectId HeadCommitId { get; }

        /// <summary>
        /// Gets the commit to be merged.
        /// </summary>
        ObjectId MergeCommitId { get; }

        /// <summary>
        /// Gets a value indicating whether a merge commit will be required.
        /// </summary>
        bool RequiresMergeCommit { get; }

        /// <summary>
        /// Gets the name of the merge source branch.
        /// </summary>
        string BranchName { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is a partial merge, that is other migration steps are required.
        /// </summary>
        bool IsPartialMerge { get; }

        /// <summary>
        /// Gets the required migrator.
        /// </summary>
        Migrator RequiredMigrator { get; }

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

        /// <summary>
        /// Applies the changes in the repository.
        /// </summary>
        /// <param name="merger">The merger.</param>
        /// <returns>The merge commit.</returns>
        ObjectId Apply(Signature merger);
    }
}