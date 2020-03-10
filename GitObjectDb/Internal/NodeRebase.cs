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
    internal sealed class NodeRebase : INodeRebase
    {
        private readonly CommitCommand _commitCommand;
        private readonly IConnectionInternal _connection;

        [FactoryDelegateConstructor(typeof(Factories.NodeRebaseFactory))]
        public NodeRebase(CommitCommand commitCommand, IConnectionInternal connection, Branch branch = null, string upstreamCommittish = null, NodeMergerPolicy policy = null)
        {
            _commitCommand = commitCommand ?? throw new ArgumentNullException(nameof(commitCommand));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Branch = branch ?? connection.Repository.Head;
            UpstreamCommit = FindUpstreamCommit(upstreamCommittish);
            Policy = policy ?? NodeMergerPolicy.Default;
            (MergeBaseCommit, ReplayedCommits) = Initialize();

            ContinueNext();
        }

        public Branch Branch { get; }

        public Commit UpstreamCommit { get; private set; }

        public NodeMergerPolicy Policy { get; }

        public Commit MergeBaseCommit { get; }

        public IImmutableList<Commit> ReplayedCommits { get; }

        public IImmutableList<Commit> CompletedCommits { get; private set; } = ImmutableList.Create<Commit>();

        public IList<NodeMergeChange> CurrentChanges { get; private set; } = new List<NodeMergeChange>();

        public RebaseStatus Status { get; private set; }

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
            var branchChanges = TreeComparer.Compare(
                _connection.Repository,
                (CompletedCommits.Count > 0 ? ReplayedCommits[CompletedCommits.Count - 1] : MergeBaseCommit).Tree,
                ReplayedCommits[CompletedCommits.Count].Tree);
            var upstreamChanges = TreeComparer.Compare(
                _connection.Repository,
                MergeBaseCommit.Tree,
                UpstreamCommit.Tree);

            CurrentChanges = NodeComparer.CollectChanges(upstreamChanges, branchChanges, Policy, isRebase: true).ToList();
            if (!CurrentChanges.Any())
            {
                ContinueNext();
            }
            else if (!CurrentChanges.Any(c => c.Status == NodeMergeStatus.EditConflict || c.Status == NodeMergeStatus.TreeConflict))
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
            if (CompletedCommits.Count == ReplayedCommits.Count)
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
            if (CurrentChanges.Any(c => c.Status == NodeMergeStatus.EditConflict || c.Status == NodeMergeStatus.TreeConflict))
            {
                throw new GitObjectDbException("Remaining conflicts were not resolved.");
            }

            var tip = CompletedCommits.LastOrDefault() ?? UpstreamCommit;
            var replayedCommit = ReplayedCommits[CompletedCommits.Count];

            // If last commit, update head so it points to the new commit
            var commit = _commitCommand.Commit(
                _connection.Repository,
                tip,
                CurrentChanges.Select(c => (Action<ObjectDatabase, TreeDefinition>)c.Transform),
                replayedCommit.Message, replayedCommit.Author, replayedCommit.Committer,
                updateHead: false);

            // Update tip if last commit
            if (CompletedCommits.Count == ReplayedCommits.Count - 1)
            {
                var logMessage = commit.BuildCommitLogMessage(false, false, false);
                _connection.Repository.UpdateTerminalReference(Branch.Reference, commit, logMessage);
            }

            CompletedCommits = CompletedCommits.Add(commit);
        }
    }
}
