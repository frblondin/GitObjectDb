using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
using LibGit2Sharp;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace GitObjectDb.Internal;

[DebuggerDisplay("Status = {Status}, ReplayedCommits = {ReplayedCommits.Count}, Completed = {CurrentStep}")]
internal sealed class Rebase : IRebase
{
    private readonly IComparerInternal _comparer;
    private readonly IMergeComparer _mergeComparer;
    private readonly CommitCommand _commitCommand;
    private readonly IConnectionInternal _connection;

    [FactoryDelegateConstructor(typeof(Factories.RebaseFactory))]
    public Rebase(IComparerInternal comparer,
                  IMergeComparer mergeComparer,
                  CommitCommand commitCommand,
                  IConnectionInternal connection,
                  string branchName,
                  string upstreamCommittish,
                  ComparisonPolicy? policy = null)
    {
        _comparer = comparer;
        _mergeComparer = mergeComparer;
        _commitCommand = commitCommand;
        _connection = connection;
        Branch = connection.Repository.Branches[branchName] ?? throw new GitObjectDbNonExistingBranchException();
        UpstreamCommit = connection.FindUpstreamCommit(upstreamCommittish, Branch);
        Policy = policy ?? connection.Model.DefaultComparisonPolicy;
        (MergeBaseCommit, ReplayedCommits) = Initialize();

        ContinueNext();
    }

    public Branch Branch { get; }

    public Commit UpstreamCommit { get; private set; }

    public ComparisonPolicy Policy { get; }

    public Commit MergeBaseCommit { get; }

    public IImmutableList<Commit> ReplayedCommits { get; }

    public int CurrentStep { get; private set; }

    public IImmutableList<Commit> CompletedCommits { get; private set; } = ImmutableList.Create<Commit>();

    public IList<MergeChange> CurrentChanges { get; private set; } = new List<MergeChange>();

    public RebaseStatus Status { get; private set; }

    private (Commit MergeBaseCommitId, IImmutableList<Commit> ReplayedCommits) Initialize()
    {
        var mergeBaseCommit = _connection.Repository.ObjectDatabase.FindMergeBase(UpstreamCommit, Branch.Tip);
        var replayedCommits = _connection.Repository.Commits.QueryBy(new CommitFilter
        {
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
            ExcludeReachableFrom = mergeBaseCommit,
            IncludeReachableFrom = Branch.Tip,
        }).ToImmutableList();
        return (mergeBaseCommit, replayedCommits);
    }

    private void ContinueNext()
    {
        var branchChanges = _comparer.Compare(
            _connection,
            CurrentStep > 0 ? ReplayedCommits[CurrentStep] : MergeBaseCommit,
            ReplayedCommits[CurrentStep],
            Policy);
        var upstreamChanges = _comparer.Compare(
            _connection,
            MergeBaseCommit,
            UpstreamCommit,
            Policy);

        CurrentChanges = _mergeComparer.Compare(upstreamChanges, branchChanges, Policy).ToList();
        if (!CurrentChanges.HasAnyConflict())
        {
            Continue();
        }
        else
        {
            Status = RebaseStatus.Conflicts;
        }
    }

    public RebaseStatus Continue()
    {
        CommitChanges();
        if (CurrentStep == -1)
        {
            Status = RebaseStatus.Complete;
        }
        else
        {
            ContinueNext();
        }
        return Status;
    }

    private void CommitChanges()
    {
        if (CurrentChanges.HasAnyConflict())
        {
            throw new GitObjectDbException("Remaining conflicts were not resolved.");
        }

        if (CurrentChanges.Any())
        {
            var commit = CommitChangesImpl();
            CompletedCommits = CompletedCommits.Add(commit);
        }
        UpdateCurrentStep();
    }

    private Commit CommitChangesImpl()
    {
        var tip = CompletedCommits.Count > 0 ?
            CompletedCommits[CompletedCommits.Count - 1] :
            UpstreamCommit;
        var replayedCommit = ReplayedCommits[CurrentStep];

        // If last commit, update branch so it points to the new commit
        var commit = _commitCommand.Commit(
            _connection,
            Branch.FriendlyName,
            tip,
            CurrentChanges.Select(c =>
                (ApplyUpdateTreeDefinition)((refTree, modules, serializer, database, treeDefinition) =>
                c.Transform(database, treeDefinition, refTree, modules, serializer))),
            new CommitDescription(replayedCommit.Message, replayedCommit.Author, replayedCommit.Committer),
            updateBranchTip: false);

        // Update tip if last commit
        if (CurrentStep == ReplayedCommits.Count - 1)
        {
            var logMessage = commit.BuildCommitLogMessage(false, false);
            _connection.Repository.UpdateBranchTip(Branch.Reference, commit, logMessage);
        }

        return commit;
    }

    private void UpdateCurrentStep()
    {
        if (CurrentStep == ReplayedCommits.Count - 1)
        {
            CurrentStep = -1;
        }
        else
        {
            CurrentStep++;
        }
    }
}
