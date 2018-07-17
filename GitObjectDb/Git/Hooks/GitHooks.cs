using GitObjectDb.Compare;
using GitObjectDb.Models;
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
        /// Occurs when a commit is about to be made.
        /// </summary>
        public event EventHandler<CommitCompletedEventArgs> CommitCompleted;

        /// <summary>
        /// Called when a commit is about to be started.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <param name="message">The message.</param>
        /// <returns>The <see cref="CancelEventArgs"/>.</returns>
        internal bool OnCommitStarted(MetadataTreeChanges changes, string message)
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
        /// Called when a commit is about to be started.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <param name="message">The message.</param>
        /// <param name="commitId">The commit identifier.</param>
        internal void OnCommitCompleted(MetadataTreeChanges changes, string message, ObjectId commitId)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var args = new CommitCompletedEventArgs(changes, message, commitId);
            CommitCompleted?.Invoke(this, args);
        }
    }
}
