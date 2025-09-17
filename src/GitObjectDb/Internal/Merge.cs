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

[DebuggerDisplay("Status = {Status}, ReplayedCommits = {ReplayedCommits.Count}, Completed = {CompletedCommits.Count}")]
internal sealed class Merge : IMerge
{
    private readonly IComparerInternal _comparer;
    private readonly IMergeComparer _mergeComparer;
    private readonly IGitUpdateCommand _gitUpdateFactory;
    private readonly ICommitCommand _commitCommand;
    private readonly IConnectionInternal _connection;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Merge(IServiceProvider serviceProvider,
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

    public bool RequiresMergeCommit { get; private set; }

    public IImmutableList<CommitEntry> Commits { get; private set; }

    public IList<MergeChange> CurrentChanges { get; private set; } = new List<MergeChange>();

    public MergeStatus Status { get; private set; }

    public CommitEntry? MergeCommit { get; private set; }

    [FactoryDelegate(typeof(Factories.MergeFactory))]
    public static async Task<IMerge> CreateAsync(IServiceProvider serviceProvider,
        IConnectionInternal connection,
        string branchName,
        string upstreamCommittish,
        ComparisonPolicy? policy = null)
    {
        var result = new Merge(serviceProvider, connection, branchName, policy);
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
        Commits = _connection.Repository.GetLogAsync(branchTip.Id.ToString(), LogOptions.Default with
        {
            SortBy = LogTraversal.FirstParentOnly | LogTraversal.Topological,
            ExcludeReachableFrom = MergeBaseCommit.Id.ToString(),
        })
            .SelectAwait(async entry => await entry.GetCommitAsync().ConfigureAwait(false))
            .ToEnumerable().ToImmutableList();
        RequiresMergeCommit = branchTip.Id != MergeBaseCommit.Id;

        await StartAsync().ConfigureAwait(false);
    }

    private async Task StartAsync()
    {
        var branchChanges = await _comparer.CompareAsync(
            _connection,
            MergeBaseCommit,
            await Branch.GetTipAsync().ConfigureAwait(false),
            Policy).ConfigureAwait(false);
        var upstreamChanges = await _comparer.CompareAsync(
            _connection,
            MergeBaseCommit,
            UpstreamCommit,
            Policy).ConfigureAwait(false);

        CurrentChanges = [.. _mergeComparer.Compare(branchChanges, upstreamChanges, Policy)];
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

    public async Task<CommitEntry> CommitAsync(Signature author, Signature committer)
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
               await CommitMergeAsync(author, committer).ConfigureAwait(false) :
               CommitFastForward();
    }

    private async Task<CommitEntry> CommitMergeAsync(Signature author, Signature committer)
    {
        var message = $"Merge {UpstreamCommit.Id} into {Branch.FriendlyName}";
        MergeCommit = await _commitCommand.CommitAsync(
            _connection,
            Branch.FriendlyName,
            CurrentChanges.Select(c => c.Transform(_gitUpdateFactory)),
            new CommitDescription(message, author, committer, mergeParent: UpstreamCommit),
            await Branch.GetTipAsync().ConfigureAwait(false)).ConfigureAwait(false);
        Status = MergeStatus.NonFastForward;
        return MergeCommit;
    }

    private CommitEntry CommitFastForward()
    {
        MergeCommit = UpstreamCommit;
        Branch.UpdateRef(UpstreamCommit);
        Status = MergeStatus.FastForward;
        return UpstreamCommit;
    }
}
