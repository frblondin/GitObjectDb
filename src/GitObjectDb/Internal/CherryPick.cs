using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GitObjectDb.Internal;

[DebuggerDisplay("Status = {Status}, ReplayedCommits = {ReplayedCommits.Count}, Completed = {CurrentStep}")]
internal sealed class CherryPick : ICherryPick
{
    private readonly IComparerInternal _comparer;
    private readonly IMergeComparer _mergeComparer;
    private readonly IGitUpdateCommand _gitUpdate;
    private readonly ICommitCommand _commitCommand;
    private readonly IConnectionInternal _connection;
    private readonly Signature? _committer;

    [FactoryDelegateConstructor(typeof(Factories.CherryPickFactory))]
    public CherryPick(IServiceProvider serviceProvider,
                      IConnectionInternal connection,
                      string branchName,
                      string committish,
                      Signature? committer,
                      CherryPickPolicy? policy = null)
    {
        _comparer = serviceProvider.GetRequiredService<IComparerInternal>();
        _mergeComparer = serviceProvider.GetRequiredService<IMergeComparer>();
        _gitUpdate = serviceProvider.GetRequiredService<IGitUpdateCommand>();
        _commitCommand = serviceProvider.GetRequiredService<ICommitCommand>();
        _connection = connection;
        _committer = committer;
        Branch = connection.Repository.Branches[branchName] ?? throw new GitObjectDbNonExistingBranchException();
        UpstreamCommit = connection.FindUpstreamCommit(committish, Branch);
        Policy = policy ?? new CherryPickPolicy(connection.Model.DefaultComparisonPolicy);

        Initialize();
    }

    public Branch Branch { get; }

    public Commit UpstreamCommit { get; private set; }

    public CherryPickPolicy Policy { get; }

    public Commit? CompletedCommit { get; private set; }

    public IList<MergeChange> CurrentChanges { get; private set; } = new List<MergeChange>();

    public CherryPickStatus Status { get; private set; } = CherryPickStatus.Conflicts;

    private void Initialize()
    {
        var mergeBaseCommit = _connection.Repository.ObjectDatabase.FindMergeBase(UpstreamCommit, Branch.Tip);
        var localChanges = _comparer.Compare(
            _connection,
            mergeBaseCommit,
            Branch.Tip,
            Policy.ComparisonPolicy);
        var changes = _comparer.Compare(
            _connection,
            UpstreamCommit.Parents.ElementAt(Policy.Mainline),
            UpstreamCommit,
            Policy.ComparisonPolicy);

        CurrentChanges = _mergeComparer.Compare(localChanges, changes, Policy.ComparisonPolicy).ToList();
        if (!CurrentChanges.HasAnyConflict())
        {
            CommitChanges();
        }
        else
        {
            Status = CherryPickStatus.Conflicts;
        }
    }

    public CherryPickStatus CommitChanges()
    {
        if (Status == CherryPickStatus.CherryPicked)
        {
            return Status;
        }
        if (CurrentChanges.HasAnyConflict())
        {
            throw new GitObjectDbException("Remaining conflicts were not resolved.");
        }

        if (CurrentChanges.Any())
        {
            CommitChangesImpl();
        }

        Status = CherryPickStatus.CherryPicked;
        return Status;
    }

    private void CommitChangesImpl()
    {
        // If last commit, update tip so it points to the new commit
        CompletedCommit = _commitCommand.Commit(
            _connection,
            Branch.FriendlyName,
            CurrentChanges.Select(c => c.Transform(_gitUpdate)),
            new(UpstreamCommit.Message, UpstreamCommit.Author, _committer ?? UpstreamCommit.Committer),
            Branch.Tip,
            updateBranchTip: false);

        // Update tip
        var logMessage = CompletedCommit.BuildCommitLogMessage(false, false);
        _connection.Repository.UpdateBranchTip(Branch.Reference, CompletedCommit, logMessage);
    }
}
