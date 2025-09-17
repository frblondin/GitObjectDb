using GitDotNet;
using GitObjectDb.Comparison;
using GitObjectDb.Model;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace GitObjectDb;

/// <summary>Represents a merge operation.</summary>
public interface IMerge
{
    /// <summary>Gets the branch in which the merge will be applied.</summary>
    Branch Branch { get; }

    /// <summary>Gets the upstream commit to be merged into <see cref="Branch"/>.</summary>
    CommitEntry UpstreamCommit { get; }

    /// <summary>Gets the merge policy.</summary>
    ComparisonPolicy Policy { get; }

    /// <summary>Gets the common commit between the two diverging branches.</summary>
    CommitEntry MergeBaseCommit { get; }

    /// <summary>Gets a value indicating whether a non fast forward merge will be required.</summary>
    bool RequiresMergeCommit { get; }

    /// <summary>Gets the commits to be merged into <see cref="Branch"/>.</summary>
    IImmutableList<CommitEntry> Commits { get; }

    /// <summary>Gets the changes required to complete the merge operation.</summary>
    IList<MergeChange> CurrentChanges { get; }

    /// <summary>Gets the merge status.</summary>
    MergeStatus Status { get; }

    /// <summary>Gets the resulting merge commit.</summary>
    CommitEntry? MergeCommit { get; }

    /// <summary>Commits the changes contained in <see cref="CurrentChanges"/>.</summary>
    /// <param name="author">The author.</param>
    /// <param name="committer">The committer.</param>
    /// <returns>The <see cref="MergeCommit"/>.</returns>
    Task<CommitEntry> CommitAsync(Signature author, Signature committer);
}
