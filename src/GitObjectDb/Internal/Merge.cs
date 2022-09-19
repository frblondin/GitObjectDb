using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
using LibGit2Sharp;
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
    private readonly UpdateTreeCommand _updateCommand;
    private readonly CommitCommand _commitCommand;
    private readonly IConnectionInternal _connection;
    private readonly string? _upstreamCommittish;

    [FactoryDelegateConstructor(typeof(Factories.MergeFactory))]
    public Merge(IComparerInternal comparer,
                 IMergeComparer mergeComparer,
                 UpdateTreeCommand updateCommand,
                 CommitCommand commitCommand,
                 IConnectionInternal connection,
                 Branch? branch = null,
                 string? upstreamCommittish = null,
                 ComparisonPolicy? policy = null)
    {
        _comparer = comparer;
        _mergeComparer = mergeComparer;
        _updateCommand = updateCommand;
        _commitCommand = commitCommand;
        _connection = connection;
        Branch = branch ?? connection.Repository.Head;
        _upstreamCommittish = upstreamCommittish;
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
            MergeBaseCommit.Tree,
            Branch.Tip.Tree,
            Policy);
        var upstreamChanges = _comparer.Compare(
            _connection,
            MergeBaseCommit.Tree,
            UpstreamCommit.Tree,
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

        // If last commit, update head so it points to the new commit
        if (RequiresMergeCommit && CurrentChanges.Any())
        {
            return CommitMerge(author, committer);
        }
        else
        {
            return CommitFastForward();
        }
    }

    private Commit CommitMerge(Signature author, Signature committer)
    {
        var message = $"Merge {_upstreamCommittish ?? UpstreamCommit.Sha} into {Branch.FriendlyName}";
        MergeCommit = _commitCommand.Commit(
            _connection,
            Branch.Tip,
            CurrentChanges.Select(c =>
                (ApplyUpdateTreeDefinition)((refTree, modules, database, treeDefinition) =>
                c.Transform(_updateCommand, database, treeDefinition, refTree, modules))),
            new CommitDescription(message, author, committer),
            updateHead: false,
            mergeParent: UpstreamCommit);
        var logMessage = MergeCommit.BuildCommitLogMessage(false, false, isMergeCommit: true);
        _connection.Repository.UpdateTerminalReference(Branch.Reference, MergeCommit, logMessage);
        Status = MergeStatus.NonFastForward;
        return MergeCommit;
    }

    private Commit CommitFastForward()
    {
        MergeCommit = UpstreamCommit;
        var logMessage = MergeCommit.BuildCommitLogMessage(false, false, false);
        _connection.Repository.UpdateTerminalReference(Branch.Reference, UpstreamCommit, logMessage);
        Status = MergeStatus.FastForward;
        return UpstreamCommit;
    }
}
