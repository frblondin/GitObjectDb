using GitObjectDb.Comparison;
using GitObjectDb.Model;
using LibGit2Sharp;
using System;

namespace GitObjectDb;

/// <summary>Represents a method that creates a <see cref="IConnection"/>.</summary>
/// <param name="path">The path containing the git repository.</param>
/// <param name="model">Model that this connection should manage.</param>
/// <param name="initialBranch">Name of the default branch name.</param>
/// <returns>A new connection instance.</returns>
public delegate IConnection ConnectionFactory(string path, IDataModel model, string initialBranch = "main");

/// <summary>Represents a GitObjectDb connection, allowing to query and perform operations on nodes.</summary>
/// <seealso cref="IDisposable" />
public interface IConnection : IQueryAccessor, IDisposable
{
    /// <summary>Gets the underlying Git repository.</summary>
    IRepository Repository { get; }

    /// <summary>Initiates a series of node transformations.</summary>
    /// <param name="branchName">The branch to apply the changes to.</param>
    /// <param name="transformations">The transformations to be applied.</param>
    /// <returns>The collection of transformations.</returns>
    ITransformationComposerWithCommit Update(string branchName,
                                             Action<ITransformationComposer>? transformations = null);

    /// <summary>Gets the index for a given branch.</summary>
    /// <param name="branchName">The branch to apply the changes to.</param>
    /// <param name="transformations">The transformations to be applied.</param>
    /// <returns>The index.</returns>
    IIndex GetIndex(string branchName,
                       Action<ITransformationComposer>? transformations = null);

    /// <summary>Compares two commits (additions, deletions, editions, conflicts).</summary>
    /// <param name="startCommittish">Starting points of comparison.</param>
    /// <param name="committish">End point of comparison.</param>
    /// <param name="policy">The merge policy to use.</param>
    /// <returns>Details about the comparison.</returns>
    ChangeCollection Compare(string startCommittish,
                             string committish,
                             ComparisonPolicy? policy = null);

    /// <summary>Rebases changes from upstream into the branch.</summary>
    /// <param name="branchName">The branch to merge changes into.</param>
    /// <param name="upstreamCommittish">The upstream committish.</param>
    /// <param name="policy">The merge policy.</param>
    /// <returns>The resut of the rebase operation.</returns>
    IRebase Rebase(string branchName,
                   string upstreamCommittish,
                   ComparisonPolicy? policy = null);

    /// <summary>Merges changes from upstream into the branch.</summary>
    /// <param name="branchName">The branch to merge changes into.</param>
    /// <param name="upstreamCommittish">The upstream committish.</param>
    /// <param name="policy">The merge policy.</param>
    /// <returns>The resut of the rebase operation.</returns>
    IMerge Merge(string branchName,
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
    ICherryPick CherryPick(string branchName,
                           string committish,
                           Signature? committer = null,
                           CherryPickPolicy? policy = null);
}

internal interface IConnectionInternal : IConnection
{
    Commit FindUpstreamCommit(string? committish, Branch branch);
}
