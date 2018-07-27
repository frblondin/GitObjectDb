using GitObjectDb.Models;
using LibGit2Sharp;
using System;

namespace GitObjectDb.Compare
{
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
        /// <returns>A <see cref="MetadataTreeChanges"/> containing all computed changes.</returns>
        MetadataTreeChanges Compare(ObjectId oldCommitId, ObjectId newCommitId);

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
        MetadataTreeChanges Compare(IObjectRepository original, IObjectRepository newRepository);
    }
}