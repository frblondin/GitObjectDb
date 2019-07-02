using GitObjectDb.Git.Hooks;
using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Models.Merge;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitObjectDb.Services
{
    /// <summary>
    /// Applies the merge changes.
    /// </summary>
    internal sealed class MergeProcessor
    {
        private readonly ComputeTreeChangesFactory _computeTreeChangesFactory;
        private readonly GitHooks _hooks;

        private readonly ObjectRepositoryMerge _merge;

        /// <summary>
        /// Initializes a new instance of the <see cref="MergeProcessor"/> class.
        /// </summary>
        /// <param name="objectRepositoryMerge">The object repository merge.</param>
        /// <param name="computeTreeChangesFactory">The <see cref="IComputeTreeChanges"/> factory.</param>
        /// <param name="hooks">The hooks.</param>
        [ActivatorUtilitiesConstructor]
        internal MergeProcessor(ObjectRepositoryMerge objectRepositoryMerge,
            ComputeTreeChangesFactory computeTreeChangesFactory, GitHooks hooks)
        {
            _merge = objectRepositoryMerge ?? throw new ArgumentNullException(nameof(objectRepositoryMerge));

            _computeTreeChangesFactory = computeTreeChangesFactory ?? throw new ArgumentNullException(nameof(computeTreeChangesFactory));
            _hooks = hooks ?? throw new ArgumentNullException(nameof(hooks));
        }

        /// <summary>
        /// Creates a new instance of <see cref="MergeProcessor"/>.
        /// </summary>
        /// <param name="objectRepositoryMerge">The object repository merge.</param>
        /// <returns>The newly created instance.</returns>
        internal delegate MergeProcessor Factory(ObjectRepositoryMerge objectRepositoryMerge);

        /// <summary>
        /// Applies the specified merger.
        /// </summary>
        /// <param name="merger">The merger.</param>
        /// <returns>The merge commit id.</returns>
        internal ObjectId Apply(Signature merger)
        {
            if (merger == null)
            {
                throw new ArgumentNullException(nameof(merger));
            }
            var remainingConflicts = _merge.ModifiedProperties.Where(c => c.IsInConflict).ToList();
            if (remainingConflicts.Any())
            {
                throw new RemainingConflictsException(remainingConflicts);
            }

            return _merge.Repository.Execute(repository =>
            {
                _merge.EnsureHeadCommit(repository);

                var resultId = ApplyMerge(merger, repository);
                if (resultId == null)
                {
                    return null;
                }
                _merge.RequiredMigrator?.Apply();
                return resultId;
            });
        }

        private ObjectId ApplyMerge(Signature merger, IRepository repository)
        {
            var computeChanges = _computeTreeChangesFactory(_merge.Repository.Container, _merge.Repository.RepositoryDescription);
            var treeChanges = computeChanges.Compute(_merge.Repository, _merge.ModifiedProperties, _merge.AddedObjects, _merge.DeletedObjects);

            if (!_hooks.OnMergeStarted(treeChanges))
            {
                return null;
            }

            var commit = CommitChanges(merger, repository, treeChanges);
            if (_merge.Repository.Container is ObjectRepositoryContainer container)
            {
                container.ReloadRepository(_merge.Repository, commit);
            }

            _hooks.OnMergeCompleted(treeChanges, commit);

            return commit;
        }

        private ObjectId CommitChanges(Signature merger, IRepository repository, ObjectRepositoryChangeCollection treeChanges)
        {
            if (_merge.RequiresMergeCommit)
            {
                var message = $"Merge branch {_merge.BranchName} into {repository.Head.FriendlyName}";
                return repository.CommitChanges(treeChanges, _merge.Serializer, message, merger, merger, hooks: _hooks, mergeParent: repository.Lookup<Commit>(_merge.MergeCommitId)).Id;
            }
            else
            {
                var commit = repository.Lookup<Commit>(_merge.MergeCommitId);
                var logMessage = commit.BuildCommitLogMessage(false, false, false);
                repository.UpdateHeadAndTerminalReference(commit, logMessage);
                return _merge.MergeCommitId;
            }
        }
    }
}
