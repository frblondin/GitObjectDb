using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using GitObjectDb.Services;
using LibGit2Sharp;
using System.Collections.Generic;

namespace GitObjectDb.Models.Compare
{
    /// <summary>
    /// Creates a new instance implementing the <see cref="IMetadataTreeMerge"/> interface.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="repositoryDescription">The repository description.</param>
    /// <param name="repository">The repository on which to apply the merge.</param>
    /// <param name="branchName">Name of the branch.</param>
    /// <returns>The newly created instance.</returns>
    public delegate IMetadataTreeMerge MetadataTreeMergeFactory(IObjectRepositoryContainer container, RepositoryDescription repositoryDescription, IObjectRepository repository, string branchName);

    /// <summary>
    /// Provides the ability to merge changes between two branches.
    /// </summary>
    public interface IMetadataTreeMerge
    {
        /// <summary>
        /// Gets the commit identifier.
        /// </summary>
        ObjectId CommitId { get; }

        /// <summary>
        /// Gets the name of the merge source branch.
        /// </summary>
        string BranchName { get; }

        /// <summary>
        /// Gets the branch target.
        /// </summary>
        ObjectId BranchTarget { get; }

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
        IList<MetadataTreeMergeChunkChange> ModifiedChunks { get; }

        /// <summary>
        /// Gets the added objects.
        /// </summary>
        IList<MetadataTreeMergeObjectAdd> AddedObjects { get; }

        /// <summary>
        /// Gets the deleted objects.
        /// </summary>
        IList<MetadataTreeMergeObjectDelete> DeletedObjects { get; }

        /// <summary>
        /// Applies the changes in the repository.
        /// </summary>
        /// <param name="merger">The merger.</param>
        /// <returns>The merge commit.</returns>
        ObjectId Apply(Signature merger);
    }
}