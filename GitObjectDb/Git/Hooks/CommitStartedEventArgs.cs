using GitObjectDb.Models.Compare;
using System;
using System.ComponentModel;

namespace GitObjectDb.Git.Hooks
{
    /// <summary>
    /// Provides data for a pre-commit event.
    /// </summary>
    /// <seealso cref="System.ComponentModel.CancelEventArgs" />
    public class CommitStartedEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitStartedEventArgs"/> class.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <param name="message">The message.</param>
        /// <exception cref="ArgumentNullException">message</exception>
        public CommitStartedEventArgs(ObjectRepositoryChanges changes, string message)
        {
            Changes = changes ?? throw new ArgumentNullException(nameof(changes));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <summary>
        /// Gets the changes.
        /// </summary>
        public ObjectRepositoryChanges Changes { get; }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message { get; }
    }
}
