using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
using LibGit2Sharp;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GitObjectDb.Internal;

[DebuggerDisplay("Status = {Status}, ReplayedCommits = {ReplayedCommits.Count}, Completed = {CurrentStep}")]
internal sealed class CherryPick : ICherryPick
{
    private readonly IComparerInternal _comparer;
    private readonly IMergeComparer _mergeComparer;
    private readonly UpdateTreeCommand _updateCommand;
    private readonly CommitCommand _commitCommand;
    private readonly IConnectionInternal _connection;
    private readonly Signature? _committer;

    [FactoryDelegateConstructor(typeof(Factories.CherryPickFactory))]
    public CherryPick(IComparerInternal comparer,
                      IMergeComparer mergeComparer,
                      UpdateTreeCommand updateCommand,
                      CommitCommand commitCommand,
                      IConnectionInternal connection,
                      string committish,
                      Signature? committer,
                      Branch? branch = null,
                      CherryPickPolicy? policy = null)
    {
        _comparer = comparer;
        _mergeComparer = mergeComparer;
        _updateCommand = updateCommand;
        _commitCommand = commitCommand;
        _connection = connection;
        _committer = committer;
        Branch = branch ?? connection.Repository.Head;
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
            mergeBaseCommit.Tree,
            Branch.Tip.Tree,
            Policy.ComparisonPolicy);
        var changes = _comparer.Compare(
            _connection,
            UpstreamCommit.Parents.ElementAt(Policy.Mainline).Tree,
            UpstreamCommit.Tree,
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
        // If last commit, update head so it points to the new commit
        CompletedCommit = _commitCommand.Commit(
            _connection,
            Branch.Tip,
            CurrentChanges.Select(c =>
                (ApplyUpdateTreeDefinition)((refTree, modules, database, treeDefinition) =>
                c.Transform(_updateCommand, database, treeDefinition, refTree, modules))),
            new(UpstreamCommit.Message, UpstreamCommit.Author, _committer ?? UpstreamCommit.Committer),
            updateHead: false);

        // Update tip
        var logMessage = CompletedCommit.BuildCommitLogMessage(false, false, false);
        _connection.Repository.UpdateTerminalReference(Branch.Reference, CompletedCommit, logMessage);
    }
}
