using GitObjectDb.Commands;
using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GitObjectDb.Internal
{
    [DebuggerDisplay("Status = {Status}, ReplayedCommits = {ReplayedCommits.Count}, Completed = {CompletedCommits.Count}")]
    internal sealed class NodeMerge : INodeMerge
    {
        private readonly CommitCommand _commitCommand;
        private readonly IConnectionInternal _connection;
        private readonly string _upstreamCommittish;

        [FactoryDelegateConstructor(typeof(Factories.NodeMergeFactory))]
        public NodeMerge(CommitCommand commitCommand, IConnectionInternal connection, Branch branch = null, string upstreamCommittish = null, NodeMergerPolicy policy = null)
        {
            _commitCommand = commitCommand ?? throw new ArgumentNullException(nameof(commitCommand));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Branch = branch ?? connection.Repository.Head;
            _upstreamCommittish = upstreamCommittish;
            UpstreamCommit = FindUpstreamCommit(upstreamCommittish);
            Policy = policy ?? NodeMergerPolicy.Default;
            (MergeBaseCommit, Commits, RequiresMergeCommit) = Initialize();

            Start();
        }

        public Branch Branch { get; }

        public Commit UpstreamCommit { get; private set; }

        public NodeMergerPolicy Policy { get; }

        public Commit MergeBaseCommit { get; }

        public bool RequiresMergeCommit { get; }

        public IImmutableList<Commit> Commits { get; }

        public IList<NodeMergeChange> CurrentChanges { get; private set; } = new List<NodeMergeChange>();

        public MergeStatus Status { get; private set; }

        public Commit MergeCommit { get; private set; }

        private Commit FindUpstreamCommit(string committish)
        {
            if (committish != null)
            {
                return (Commit)_connection.Repository.Lookup(committish) ??
                    throw new GitObjectDbException($"Upstream commit '{committish}' could not be resolved.");
            }
            else if (string.IsNullOrEmpty(Branch.UpstreamBranchCanonicalName))
            {
                throw new GitObjectDbException("Branch has no upstream branch defined.");
            }
            else
            {
                return _connection.Repository.Branches[Branch.UpstreamBranchCanonicalName].Tip;
            }
        }

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
            var branchChanges = TreeComparer.Compare(
                _connection.Repository,
                MergeBaseCommit.Tree,
                Branch.Tip.Tree);
            var upstreamChanges = TreeComparer.Compare(
                _connection.Repository,
                MergeBaseCommit.Tree,
                UpstreamCommit.Tree);

            CurrentChanges = NodeComparer.CollectChanges(branchChanges, upstreamChanges, Policy, isRebase: false).ToList();
            if (!CurrentChanges.Any())
            {
                Status = MergeStatus.UpToDate;
            }
            else if (!CurrentChanges.Any(c => c.Status == NodeMergeStatus.EditConflict || c.Status == NodeMergeStatus.TreeConflict))
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
            if (CurrentChanges.Any(c => c.Status == NodeMergeStatus.EditConflict || c.Status == NodeMergeStatus.TreeConflict))
            {
                throw new GitObjectDbException("Remaining conflicts were not resolved.");
            }

            // If last commit, update head so it points to the new commit
            if (RequiresMergeCommit && CurrentChanges.Any())
            {
                var message = $"Merge {_upstreamCommittish} into {Branch.FriendlyName}";
                MergeCommit = _commitCommand.Commit(
                    _connection.Repository,
                    Branch.Tip,
                    CurrentChanges.Select(c => (Action<ObjectDatabase, TreeDefinition>)c.Transform),
                    message, author, committer,
                    updateHead: false,
                    mergeParent: UpstreamCommit);
                var logMessage = MergeCommit.BuildCommitLogMessage(false, false, isMergeCommit: true);
                _connection.Repository.UpdateTerminalReference(Branch.Reference, MergeCommit, logMessage);
                Status = MergeStatus.NonFastForward;
                return MergeCommit;
            }
            else
            {
                MergeCommit = UpstreamCommit;
                var logMessage = MergeCommit.BuildCommitLogMessage(false, false, false);
                _connection.Repository.UpdateTerminalReference(Branch.Reference, UpstreamCommit, logMessage);
                Status = MergeStatus.FastForward;
                return UpstreamCommit;
            }
        }
    }
}
