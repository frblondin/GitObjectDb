using GitObjectDb.Comparison;
using LibGit2Sharp;
using System.Collections.Generic;

namespace GitObjectDb
{
    /// <summary>Reports the result of a cherry pick.</summary>
    public interface ICherryPick
    {
        /// <summary>Gets the branch in which the cherry-pick will be applied.</summary>
        Branch Branch { get; }

        /// <summary>Gets the upstream commit to be cherry picked.</summary>
        Commit UpstreamCommit { get; }

        /// <summary>Gets the merge policy.</summary>
        CherryPickPolicy Policy { get; }

        /// <summary>Gets the current changes involved for replaying the commit.</summary>
        IList<MergeChange> CurrentChanges { get; }

        /// <summary>
        /// Gets the resulting commit of the cherry-pick.
        /// This will return null if the cherry pick was not committed.
        /// This can happen if: 1) The cherry pick resulted in conflicts.
        /// 2) The option to not commit on success is set.
        /// </summary>
        Commit? CompletedCommit { get; }

        /// <summary>Gets the status of the cherry pick.</summary>
        CherryPickStatus Status { get; }

        /// <summary>Commits current changes.</summary>
        /// <returns>The resulting commit of the cherry-pick operation.</returns>
        CherryPickStatus CommitChanges();
    }
}
