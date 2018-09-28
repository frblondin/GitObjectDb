using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Models.Rebase
{
    /// <summary>
    /// Information on a rebase operation.
    /// </summary>
    public class ObjectRepositoryRebaseResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryRebaseResult"/> class.
        /// </summary>
        /// <param name="rebaseResult">The rebase result.</param>
        internal ObjectRepositoryRebaseResult(RebaseResult rebaseResult)
        {
            Status = rebaseResult.Status;
            CompletedStepCount = rebaseResult.CompletedStepCount;
            TotalStepCount = rebaseResult.TotalStepCount;
        }

        /// <summary>
        /// Gets whether the rebase operation run until it should stop (completed the rebase,
        /// or the operation for the current step is one that sequencing should stop.
        /// </summary>
        public RebaseStatus Status { get; }

        /// <summary>
        /// Gets the number of completed steps.
        /// </summary>
        public virtual long CompletedStepCount { get; }

        /// <summary>
        /// Gets the total number of steps in the rebase.
        /// </summary>
        public virtual long TotalStepCount { get; }
    }
}
