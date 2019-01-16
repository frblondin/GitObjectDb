using GitObjectDb.Models.Compare;
using LibGit2Sharp;
using System;
using System.ComponentModel;

namespace GitObjectDb.Git.Hooks
{
    /// <summary>
    /// Provides data for a post-merge event.
    /// </summary>
    public class MergeCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MergeCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <param name="commitId">The commit identifier.</param>
        /// <exception cref="ArgumentNullException">message</exception>
        public MergeCompletedEventArgs(ObjectRepositoryChangeCollection changes, ObjectId commitId)
        {
            Changes = changes ?? throw new ArgumentNullException(nameof(changes));
            CommitId = commitId ?? throw new ArgumentNullException(nameof(commitId));
        }

        /// <summary>
        /// Gets the changes.
        /// </summary>
        public ObjectRepositoryChangeCollection Changes { get; }

        /// <summary>
        /// Gets the commit identifier.
        /// </summary>
        public ObjectId CommitId { get; }
    }
}
