using GitDotNet;
using GitObjectDb.Comparison;
using GitObjectDb.Model;
using System;
using System.Threading.Tasks;

namespace GitObjectDb;

/// <summary>Represents a method that creates a <see cref="IConnection"/>.</summary>
/// <param name="path">The path containing the git repository.</param>
/// <param name="model">Model that this connection should manage.</param>
/// <returns>A new connection instance.</returns>
public delegate IConnection ConnectionFactory(string path, IDataModel model);

/// <summary>Represents a GitObjectDb connection, allowing to query and perform operations on nodes.</summary>
/// <seealso cref="IDisposable" />
public interface IConnection : IDataProvider, IDisposable
{
    /// <summary>Gets the underlying Git repository.</summary>
    IGitConnection Repository { get; }

    /// <summary>Initiates a series of node transformations.</summary>
    /// <param name="branchName">The branch to apply the changes to.</param>
    /// <param name="transformations">The transformations to be applied.</param>
    /// <returns>The collection of transformations.</returns>
    Task<IChangeComposerWithCommit> UpdateAsync(string branchName,
        Func<IChangeComposer, Task>? transformations = null);

    /// <summary>Gets the index for a given branch.</summary>
    /// <param name="branchName">The branch to apply the changes to.</param>
    /// <param name="transformations">The transformations to be applied.</param>
    /// <returns>The index.</returns>
    Task<IIndex> GetIndexAsync(string branchName,
        Func<IChangeComposer, Task>? transformations = null);

    /// <summary>Compares two commits (additions, deletions, editions, conflicts).</summary>
    /// <param name="startCommittish">Starting points of comparison.</param>
    /// <param name="committish">End point of comparison.</param>
    /// <param name="policy">The merge policy to use.</param>
    /// <returns>Details about the comparison.</returns>
    Task<ChangeCollection> CompareAsync(string startCommittish,
        string committish,
        ComparisonPolicy? policy = null);

    /// <summary>Rebases changes from upstream into the branch.</summary>
    /// <param name="branchName">The branch to merge changes into.</param>
    /// <param name="upstreamCommittish">The upstream committish.</param>
    /// <param name="policy">The merge policy.</param>
    /// <returns>The resut of the rebase operation.</returns>
    Task<IRebase> RebaseAsync(string branchName,
        string upstreamCommittish,
        ComparisonPolicy? policy = null);

    /// <summary>Merges changes from upstream into the branch.</summary>
    /// <param name="branchName">The branch to merge changes into.</param>
    /// <param name="upstreamCommittish">The upstream committish.</param>
    /// <param name="policy">The merge policy.</param>
    /// <returns>The resut of the rebase operation.</returns>
    Task<IMerge> MergeAsync(string branchName,
        string upstreamCommittish,
        ComparisonPolicy? policy = null);

    /// <summary>
    /// Cherry-picks the specified commit.
    /// </summary>
    /// <param name="branchName">The branch to cherry-pick into.</param>
    /// <param name="committish">The commit to cherry-pick.</param>
    /// <param name="committer">The user who is performing the cherry pick.</param>
    /// <param name="policy">The cherry-pick policy.</param>
    /// <returns>The result of the cherry-pick operation.</returns>
    Task<ICherryPick> CherryPickAsync(string branchName,
        string committish,
        Signature? committer = null,
        CherryPickPolicy? policy = null);
}

internal interface IConnectionInternal : IConnection
{
    Task<CommitEntry> FindUpstreamCommitAsync(string? committish, Branch branch);
}
