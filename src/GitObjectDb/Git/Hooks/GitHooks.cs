using GitObjectDb.Models.Compare;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GitObjectDb.Git.Hooks
{
    /// <summary>
    /// Allows listeners to subscribe to Git events.
    /// </summary>
    public class GitHooks
    {
        /// <summary>
        /// Occurs when a commit is about to be made.
        /// </summary>
        public event EventHandler<CommitStartedEventArgs> CommitStarted;

        /// <summary>
        /// Occurs when a commit has been completed successfully.
        /// </summary>
        public event EventHandler<CommitCompletedEventArgs> CommitCompleted;

        /// <summary>
        /// Occurs when a merge is about to be processed.
        /// </summary>
        public event EventHandler<MergeStartedEventArgs> MergeStarted;

        /// <summary>
        /// Occurs when a merge has been completed successfully.
        /// </summary>
        public event EventHandler<MergeCompletedEventArgs> MergeCompleted;

        /// <summary>
        /// Called when a commit is about to be started.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <param name="message">The message.</param>
        /// <returns>The <see cref="CancelEventArgs"/>.</returns>
        internal bool OnCommitStarted(ObjectRepositoryChangeCollection changes, string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var args = new CommitStartedEventArgs(changes, message);
            CommitStarted?.Invoke(this, args);
            return !args.Cancel;
        }

        /// <summary>
        /// Called when a commit has been completed successfully.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <param name="message">The message.</param>
        /// <param name="commitId">The commit identifier.</param>
        internal void OnCommitCompleted(ObjectRepositoryChangeCollection changes, string message, ObjectId commitId)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var args = new CommitCompletedEventArgs(changes, message, commitId);
            CommitCompleted?.Invoke(this, args);
        }

        /// <summary>
        /// Called when a merge is about to be processed.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <returns>The <see cref="CancelEventArgs"/>.</returns>
        internal bool OnMergeStarted(ObjectRepositoryChangeCollection changes)
        {
            var args = new MergeStartedEventArgs(changes);
            MergeStarted?.Invoke(this, args);
            return !args.Cancel;
        }

        /// <summary>
        /// Called when a merge has been completed successfully.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <param name="commitId">The commit identifier.</param>
        internal void OnMergeCompleted(ObjectRepositoryChangeCollection changes, ObjectId commitId)
        {
            var args = new MergeCompletedEventArgs(changes, commitId);
            MergeCompleted?.Invoke(this, args);
        }
    }
}
