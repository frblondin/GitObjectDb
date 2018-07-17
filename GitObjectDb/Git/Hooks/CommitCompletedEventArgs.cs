using GitObjectDb.Compare;
using LibGit2Sharp;
using System;
using System.ComponentModel;

namespace GitObjectDb.Git.Hooks
{
    /// <summary>
    /// Provides data for a pre-commit event.
    /// </summary>
    /// <seealso cref="System.ComponentModel.CancelEventArgs" />
    public class CommitCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <param name="message">The message.</param>
        /// <param name="commitId">The commit identifier.</param>
        /// <exception cref="ArgumentNullException">message</exception>
        public CommitCompletedEventArgs(MetadataTreeChanges changes, string message, ObjectId commitId)
        {
            Changes = changes ?? throw new ArgumentNullException(nameof(changes));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            CommitId = commitId ?? throw new ArgumentNullException(nameof(commitId));
        }

        /// <summary>
        /// Gets the changes.
        /// </summary>
        public MetadataTreeChanges Changes { get; }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the commit identifier.
        /// </summary>
        public ObjectId CommitId { get; }
    }
}
