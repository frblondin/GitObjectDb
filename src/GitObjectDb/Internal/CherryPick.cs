using GitDotNet;
using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private CherryPick(IServiceProvider serviceProvider,
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        IConnectionInternal connection,
        string branchName,
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
        Policy = policy ?? new CherryPickPolicy(connection.Model.DefaultComparisonPolicy);
    }

    public Branch Branch { get; }

    public CommitEntry UpstreamCommit { get; private set; }

    public CherryPickPolicy Policy { get; }

    public CommitEntry? CompletedCommit { get; private set; }

    public IList<MergeChange> CurrentChanges { get; private set; } = new List<MergeChange>();

    public CherryPickStatus Status { get; private set; } = CherryPickStatus.Conflicts;

    [FactoryDelegate(typeof(Factories.CherryPickFactory))]
    public static async Task<ICherryPick> CreateAsync(IServiceProvider serviceProvider,
        IConnectionInternal connection,
        string branchName,
        string committish,
        Signature? committer,
        CherryPickPolicy? policy = null)
    {
        var result = new CherryPick(serviceProvider, connection, branchName, committer, policy);
        await result.InitializeAsync(committish).ConfigureAwait(false);
        return result;
    }

    private async Task InitializeAsync(string committish)
    {
        UpstreamCommit = await _connection.FindUpstreamCommitAsync(committish, Branch).ConfigureAwait(false);
        var tip = await Branch.GetTipAsync().ConfigureAwait(false);
        var mergeBaseCommit = await _connection.Repository.GetMergeBaseAsync(
            UpstreamCommit.Id.ToString(), tip.Id.ToString()).ConfigureAwait(false) ??
            throw new GitObjectDbException("No merge base found between the two commits.");
        var localChanges = await _comparer.CompareAsync(
            _connection,
            mergeBaseCommit,
            tip,
            Policy.ComparisonPolicy).ConfigureAwait(false);
        var parent = await _connection.Repository.Objects.GetAsync<CommitEntry>(
            UpstreamCommit.ParentIds.ElementAt(Policy.Mainline)).ConfigureAwait(false);
        var changes = await _comparer.CompareAsync(_connection, parent, UpstreamCommit, Policy.ComparisonPolicy).ConfigureAwait(false);

        CurrentChanges = _mergeComparer.Compare(localChanges, changes, Policy.ComparisonPolicy).ToList();
        if (!CurrentChanges.HasAnyConflict())
        {
            await CommitChangesAsync().ConfigureAwait(false);
        }
        else
        {
            Status = CherryPickStatus.Conflicts;
        }
    }

    public async Task<CherryPickStatus> CommitChangesAsync()
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
            await CommitChangesImplAsync().ConfigureAwait(false);
        }

        Status = CherryPickStatus.CherryPicked;
        return Status;
    }

    private async Task CommitChangesImplAsync()
    {
        CompletedCommit = await _commitCommand.CommitAsync(
            _connection,
            Branch.FriendlyName,
            CurrentChanges.Select(c => c.Transform(_gitUpdate)),
            new(UpstreamCommit.Message, UpstreamCommit.Author, _committer ?? UpstreamCommit.Committer),
            await Branch.GetTipAsync().ConfigureAwait(false)).ConfigureAwait(false);
    }
}
