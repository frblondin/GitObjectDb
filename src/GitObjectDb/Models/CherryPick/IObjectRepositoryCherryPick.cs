using GitObjectDb.Models.Compare;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace GitObjectDb.Models.CherryPick
{
    /// <summary>
    /// Creates a new instance implementing the <see cref="IObjectRepositoryCherryPick"/> interface.
    /// </summary>
    /// <param name="repository">The repository on which to apply the merge.</param>
    /// <param name="commitId">The commit to be cherry picked.</param>
    /// <returns>The newly created instance.</returns>
    public delegate IObjectRepositoryCherryPick ObjectRepositoryCherryPickFactory(IObjectRepository repository, ObjectId commitId);

    /// <summary>
    /// Encapsulates a cherry pick operation.
    /// </summary>
    public interface IObjectRepositoryCherryPick
    {
        /// <summary>
        /// Gets the head commit identifier.
        /// </summary>
        ObjectId HeadCommitId { get; }

        /// <summary>
        /// Gets the commit to be cherry picked.
        /// </summary>
        ObjectId CherryPickCommitId { get; }

        /// <summary>
        /// Gets whether the cherry pick has any conflict or if it has completed.
        /// </summary>
        CherryPickStatus Status { get; }

        /// <summary>
        /// Gets the modified chunks.
        /// </summary>
        IList<ObjectRepositoryPropertyChange> ModifiedProperties { get; }

        /// <summary>
        /// Gets the added objects.
        /// </summary>
        IList<ObjectRepositoryAdd> AddedObjects { get; }

        /// <summary>
        /// Gets the deleted objects.
        /// </summary>
        IList<ObjectRepositoryDelete> DeletedObjects { get; }

        /// <summary>
        /// Gets the result of the cherry pick operation.
        /// </summary>
        IObjectRepository Result { get; }

        /// <summary>
        /// Completes the cherry pick operation.
        /// </summary>
        /// <returns>The new instance of repository containing reflected changes.</returns>
        IObjectRepository Commit();
    }
}