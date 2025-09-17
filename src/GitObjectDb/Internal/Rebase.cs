using GitDotNet;
using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Internal;

[DebuggerDisplay("Status = {Status}, ReplayedCommits = {ReplayedCommits.Count}, Completed = {CurrentStep}")]
internal sealed class Rebase : IRebase
{
    private readonly IComparerInternal _comparer;
    private readonly IMergeComparer _mergeComparer;
    private readonly IGitUpdateCommand _gitUpdateFactory;
    private readonly ICommitCommand _commitCommand;
    private readonly IConnectionInternal _connection;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Rebase(IServiceProvider serviceProvider,
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
                  IConnectionInternal connection,
                  string branchName,
                  ComparisonPolicy? policy = null)
    {
        _comparer = serviceProvider.GetRequiredService<IComparerInternal>();
        _mergeComparer = serviceProvider.GetRequiredService<IMergeComparer>();
        _gitUpdateFactory = serviceProvider.GetRequiredService<IGitUpdateCommand>();
        _commitCommand = serviceProvider.GetRequiredService<ICommitCommand>();
        _connection = connection;
        Branch = connection.Repository.Branches[branchName] ?? throw new GitObjectDbNonExistingBranchException();
        Policy = policy ?? connection.Model.DefaultComparisonPolicy;
    }

    public Branch Branch { get; }

    public CommitEntry UpstreamCommit { get; private set; }

    public ComparisonPolicy Policy { get; }

    public CommitEntry MergeBaseCommit { get; private set; }

    public IImmutableList<CommitEntry> ReplayedCommits { get; private set; }

    public int CurrentStep { get; private set; }

    public IImmutableList<CommitEntry> CompletedCommits { get; private set; } = ImmutableList.Create<CommitEntry>();

    public IList<MergeChange> CurrentChanges { get; private set; } = new List<MergeChange>();

    public RebaseStatus Status { get; private set; }

    [FactoryDelegate(typeof(Factories.RebaseFactory))]
    public static async Task<IRebase> CreateAsync(IServiceProvider serviceProvider,
        IConnectionInternal connection,
        string branchName,
        string upstreamCommittish,
        ComparisonPolicy? policy = null)
    {
        var result = new Rebase(serviceProvider, connection, branchName, policy);
        await result.InitializeAsync(upstreamCommittish).ConfigureAwait(false);
        return result;
    }

    private async Task InitializeAsync(string upstreamCommittish)
    {
        UpstreamCommit = await _connection.FindUpstreamCommitAsync(upstreamCommittish, Branch).ConfigureAwait(false);
        var branchTip = await Branch.GetTipAsync().ConfigureAwait(false);
        MergeBaseCommit = await _connection.Repository.GetMergeBaseAsync(
            UpstreamCommit.Id.ToString(), branchTip.Id.ToString()).ConfigureAwait(false) ??
            throw new GitObjectDbException("No merge base found between the two commits.");
        ReplayedCommits = _connection.Repository.GetLogAsync(branchTip.Id.ToString(), LogOptions.Default with
        {
            SortBy = LogTraversal.FirstParentOnly | LogTraversal.Topological,
            ExcludeReachableFrom = MergeBaseCommit.Id.ToString(),
        })
            .SelectAwait(async entry => await entry.GetCommitAsync().ConfigureAwait(false))
            .ToEnumerable().ToImmutableList();

        if (ReplayedCommits.Any())
        {
            await ContinueNextAsync().ConfigureAwait(false);
        }
        else
        {
            Branch.UpdateRef(UpstreamCommit);
            Status = RebaseStatus.Complete;
        }
    }

    private async Task ContinueNextAsync()
    {
        if (ReplayedCommits.Any())
        {
            var branchChanges = await _comparer.CompareAsync(
                _connection,
                CurrentStep > 0 ? ReplayedCommits[CurrentStep] : MergeBaseCommit,
                ReplayedCommits[CurrentStep],
                Policy).ConfigureAwait(false);
            var upstreamChanges = await _comparer.CompareAsync(
                _connection,
                MergeBaseCommit,
                UpstreamCommit,
                Policy).ConfigureAwait(false);

            CurrentChanges = _mergeComparer.Compare(upstreamChanges, branchChanges, Policy).ToList();
        }
        if (!CurrentChanges.HasAnyConflict())
        {
            await ContinueAsync().ConfigureAwait(false);
        }
        else
        {
            Status = RebaseStatus.Conflicts;
        }
    }

    public async Task<RebaseStatus> ContinueAsync()
    {
        await CommitChangesAsync().ConfigureAwait(false);
        if (CurrentStep == -1)
        {
            Status = RebaseStatus.Complete;
        }
        else
        {
            await ContinueNextAsync().ConfigureAwait(false);
        }
        return Status;
    }

    private async Task CommitChangesAsync()
    {
        if (CurrentChanges.HasAnyConflict())
        {
            throw new GitObjectDbException("Remaining conflicts were not resolved.");
        }

        if (CurrentChanges.Any())
        {
            var commit = await CommitChangesImplAsync().ConfigureAwait(false);
            CompletedCommits = CompletedCommits.Add(commit);
        }
        UpdateCurrentStep();
    }

    private async Task<CommitEntry> CommitChangesImplAsync()
    {
        var tip = CompletedCommits.Count > 0 ?
            CompletedCommits[CompletedCommits.Count - 1] :
            UpstreamCommit;
        var replayedCommit = ReplayedCommits[CurrentStep];

        // If last commit, update branch so it points to the new commit
        var commit = await _commitCommand.CommitAsync(
            _connection,
            Branch.FriendlyName,
            CurrentChanges.Select(c => c.Transform(_gitUpdateFactory)),
            new CommitDescription(replayedCommit.Message, replayedCommit.Author, replayedCommit.Committer),
            tip).ConfigureAwait(false);

        // Update tip if last commit
        if (CurrentStep == ReplayedCommits.Count - 1)
        {
            Branch.UpdateRef(commit);
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
