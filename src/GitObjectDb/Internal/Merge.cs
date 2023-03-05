using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace GitObjectDb.Internal;

[DebuggerDisplay("Status = {Status}, ReplayedCommits = {ReplayedCommits.Count}, Completed = {CompletedCommits.Count}")]
internal sealed class Merge : IMerge
{
    private readonly IComparerInternal _comparer;
    private readonly IMergeComparer _mergeComparer;
    private readonly IGitUpdateCommand _gitUpdateFactory;
    private readonly ICommitCommand _commitCommand;
    private readonly IConnectionInternal _connection;

    [FactoryDelegateConstructor(typeof(Factories.MergeFactory))]
    public Merge(IServiceProvider serviceProvider,
                 IConnectionInternal connection,
                 string branchName,
                 string upstreamCommittish,
                 ComparisonPolicy? policy = null)
    {
        _comparer = serviceProvider.GetRequiredService<IComparerInternal>();
        _mergeComparer = serviceProvider.GetRequiredService<IMergeComparer>();
        _gitUpdateFactory = serviceProvider.GetRequiredService<IGitUpdateCommand>();
        _commitCommand = serviceProvider.GetRequiredService<ICommitCommand>();
        _connection = connection;
        Branch = connection.Repository.Branches[branchName] ?? throw new GitObjectDbNonExistingBranchException();
        UpstreamCommit = connection.FindUpstreamCommit(upstreamCommittish, Branch);
        Policy = policy ?? connection.Model.DefaultComparisonPolicy;
        (MergeBaseCommit, Commits, RequiresMergeCommit) = Initialize();

        Start();
    }

    public Branch Branch { get; }

    public Commit UpstreamCommit { get; private set; }

    public ComparisonPolicy Policy { get; }

    public Commit MergeBaseCommit { get; }

    public bool RequiresMergeCommit { get; }

    public IImmutableList<Commit> Commits { get; }

    public IList<MergeChange> CurrentChanges { get; private set; } = new List<MergeChange>();

    public MergeStatus Status { get; private set; }

    public Commit? MergeCommit { get; private set; }

    private (Commit MergeBaseCommitId, IImmutableList<Commit> ReplayedCommits, bool RequiresMergeCommit) Initialize()
    {
        var mergeBaseCommit = _connection.Repository.ObjectDatabase.FindMergeBase(UpstreamCommit, Branch.Tip);
        var replayedCommits = _connection.Repository.Commits.QueryBy(new CommitFilter
        {
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
            ExcludeReachableFrom = mergeBaseCommit,
            IncludeReachableFrom = Branch.Tip,
        }).ToImmutableList();
        return (mergeBaseCommit, replayedCommits, Branch.Tip.Id != mergeBaseCommit.Id);
    }

    private void Start()
    {
        var branchChanges = _comparer.Compare(
            _connection,
            MergeBaseCommit,
            Branch.Tip,
            Policy);
        var upstreamChanges = _comparer.Compare(
            _connection,
            MergeBaseCommit,
            UpstreamCommit,
            Policy);

        CurrentChanges = _mergeComparer.Compare(branchChanges, upstreamChanges, Policy).ToList();
        if (!CurrentChanges.Any())
        {
            Status = MergeStatus.UpToDate;
        }
        else if (!CurrentChanges.HasAnyConflict())
        {
            Status = RequiresMergeCommit ? MergeStatus.NonFastForward : MergeStatus.FastForward;
        }
        else
        {
            Status = MergeStatus.Conflicts;
        }
    }

    public Commit Commit(Signature author, Signature committer)
    {
        if (MergeCommit != null)
        {
            throw new GitObjectDbException("Merge is already completed.");
        }
        if (CurrentChanges.HasAnyConflict())
        {
            throw new GitObjectDbException("Remaining conflicts were not resolved.");
        }

        // If last commit, update branch so it points to the new commit
        return RequiresMergeCommit && CurrentChanges.Any() ?
               CommitMerge(author, committer) :
               CommitFastForward();
    }

    private Commit CommitMerge(Signature author, Signature committer)
    {
        var message = $"Merge {UpstreamCommit.Sha} into {Branch.FriendlyName}";
        MergeCommit = _commitCommand.Commit(
            _connection,
            Branch.FriendlyName,
            CurrentChanges.Select(c => c.Transform(_gitUpdateFactory)),
            new CommitDescription(message, author, committer, mergeParent: UpstreamCommit),
            Branch.Tip);
        var logMessage = MergeCommit.BuildCommitLogMessage(false, isMergeCommit: true);
        _connection.Repository.UpdateBranchTip(Branch.Reference, MergeCommit, logMessage);
        Status = MergeStatus.NonFastForward;
        return MergeCommit;
    }

    private Commit CommitFastForward()
    {
        MergeCommit = UpstreamCommit;
        var logMessage = MergeCommit.BuildCommitLogMessage(false, false);
        _connection.Repository.UpdateBranchTip(Branch.Reference, UpstreamCommit, logMessage);
        Status = MergeStatus.FastForward;
        return UpstreamCommit;
    }
}
