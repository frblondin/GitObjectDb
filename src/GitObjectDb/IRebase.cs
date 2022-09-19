using GitObjectDb.Comparison;
using LibGit2Sharp;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace GitObjectDb;

/// <summary>Represents a rebase operation.</summary>
public interface IRebase
{
    /// <summary>Gets the branch in which the rebase will be applied.</summary>
    Branch Branch { get; }

    /// <summary>Gets the upstream commit to be rebased into <see cref="Branch"/>.</summary>
    Commit UpstreamCommit { get; }

    /// <summary>Gets the merge policy.</summary>
    ComparisonPolicy Policy { get; }

    /// <summary>Gets the common commit between the two diverging branches.</summary>
    Commit MergeBaseCommit { get; }

    /// <summary>Gets the commits that will be replayed during this rebase operation.</summary>
    IImmutableList<Commit> ReplayedCommits { get; }

    /// <summary>Gets the current commit index being processed in <see cref="ReplayedCommits"/>.</summary>
    int CurrentStep { get; }

    /// <summary>Gets the commits that were completed during the ongoing rebase operation.</summary>
    IImmutableList<Commit> CompletedCommits { get; }

    /// <summary>Gets the current changes involved for replaying the current commit.</summary>
    IList<MergeChange> CurrentChanges { get; }

    /// <summary>Gets the rebase status.</summary>
    RebaseStatus Status { get; }

    /// <summary>Commits current changes and move to the next commit.</summary>
    /// <returns>The new status after continuing the rebase operation.</returns>
    RebaseStatus Continue();
}
