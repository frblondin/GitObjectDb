using GitObjectDb.Attributes;
using GitObjectDb.Git;
using GitObjectDb.Models.Compare;
using GitObjectDb.Models.Migration;
using GitObjectDb.Services;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models.Rebase
{
    /// <inheritdoc />
    [ExcludeFromGuardForNull]
    internal class ObjectRepositoryRebase : IObjectRepositoryRebase
    {
        private readonly IObjectRepositoryLoader _repositoryLoader;
        private readonly MigrationScaffolderFactory _migrationScaffolderFactory;
        private readonly RebaseProcessor.Factory _rebaseProcessorFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryRebase"/> class.
        /// </summary>
        /// <param name="repository">The repository on which to apply the merge.</param>
        /// <param name="rebaseCommitId">The commit to be rebased.</param>
        /// <param name="branchName">Name of the branch.</param>
        /// <param name="repositoryLoader">The repository loader.</param>
        /// <param name="migrationScaffolderFactory">The <see cref="MigrationScaffolder"/> factory.</param>
        /// <param name="rebaseProcessorFactory">The <see cref="MergeProcessor"/> factory.</param>
        [ActivatorUtilitiesConstructor]
        public ObjectRepositoryRebase(IObjectRepository repository, ObjectId rebaseCommitId, string branchName,
            IObjectRepositoryLoader repositoryLoader, MigrationScaffolderFactory migrationScaffolderFactory,
            RebaseProcessor.Factory rebaseProcessorFactory)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            HeadCommitId = repository.CommitId ?? throw new GitObjectDbException("Repository instance is not linked to any commit.");
            RebaseCommitId = rebaseCommitId ?? throw new ArgumentNullException(nameof(rebaseCommitId));
            BranchName = branchName ?? throw new ArgumentNullException(nameof(branchName));

            _repositoryLoader = repositoryLoader ?? throw new ArgumentNullException(nameof(repositoryLoader));
            _migrationScaffolderFactory = migrationScaffolderFactory ?? throw new ArgumentNullException(nameof(migrationScaffolderFactory));
            _rebaseProcessorFactory = rebaseProcessorFactory ?? throw new ArgumentNullException(nameof(rebaseProcessorFactory));

            Initialize();
        }

        /// <summary>
        /// Gets the repository.
        /// </summary>
        public IObjectRepository Repository { get; }

        /// <inheritdoc/>
        public ObjectId HeadCommitId { get; }

        /// <inheritdoc/>
        public ObjectId RebaseCommitId { get; private set; }

        /// <inheritdoc/>
        public string BranchName { get; }

        /// <inheritdoc/>
        public IImmutableList<ObjectId> ReplayedCommits { get; private set; }

        /// <inheritdoc/>
        public RebaseStatus Status { get; private set; }

        /// <inheritdoc/>
        public int CompletedStepCount { get; internal set; }

        /// <inheritdoc/>
        public int TotalStepCount => ReplayedCommits?.Count ?? 0;

        /// <inheritdoc/>
        public IList<ObjectRepositoryPropertyChange> ModifiedProperties { get; } = new List<ObjectRepositoryPropertyChange>();

        /// <inheritdoc/>
        public IList<ObjectRepositoryAdd> AddedObjects { get; } = new List<ObjectRepositoryAdd>();

        /// <inheritdoc/>
        public IList<ObjectRepositoryDelete> DeletedObjects { get; } = new List<ObjectRepositoryDelete>();

        /// <summary>
        /// Gets the start repository.
        /// </summary>
        internal IObjectRepository StartRepository { get; private set; }

        /// <summary>
        /// Gets the merge base commit identifier.
        /// </summary>
        internal ObjectId MergeBaseCommitId { get; private set; }

        /// <summary>
        /// Gets the transformations.
        /// </summary>
        internal IList<IObjectRepository> Transformations { get; } = new List<IObjectRepository>();

        /// <summary>
        /// Gets the modified upstream branch entries.
        /// </summary>
        internal IImmutableList<TreeEntryChanges> ModifiedUpstreamBranchEntries { get; private set; }

        private void Initialize()
        {
            Repository.Execute(repository =>
            {
                EnsureHeadCommit(repository);

                var rebaseCommit = repository.Lookup<Commit>(RebaseCommitId);
                var headTip = repository.Head.Tip;
                var mergeBaseCommit = repository.ObjectDatabase.FindMergeBase(headTip, rebaseCommit);
                MergeBaseCommitId = mergeBaseCommit.Id;

                EnsureNoMigrations();
                ReplayedCommits = repository.Commits.QueryBy(new CommitFilter
                {
                    SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
                    ExcludeReachableFrom = rebaseCommit,
                    IncludeReachableFrom = headTip,
                }).Select(c => c.Id).ToImmutableList();
                UpdateUpstreamBranchModifiedPaths(repository, rebaseCommit, mergeBaseCommit);

                StartRepository = _repositoryLoader.LoadFrom(Repository.Container, Repository.RepositoryDescription, RebaseCommitId);

                Status = _rebaseProcessorFactory(this).ContinueNext(repository);
            });
        }

        /// <summary>
        /// Ensures that the head tip refers to the right commit.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <exception cref="GitObjectDbException">The current head commit id is different from the commit used by current repository.</exception>
        internal void EnsureHeadCommit(IRepository repository)
        {
            if (!repository.Head.Tip.Id.Equals(HeadCommitId))
            {
                throw new GitObjectDbException("The current head commit id is different from the commit used by current repository.");
            }
        }

        private void EnsureNoMigrations()
        {
            var migrationScaffolder = _migrationScaffolderFactory(Repository.Container, Repository.RepositoryDescription);
            var upstreamBranchMigrators = migrationScaffolder.Scaffold(MergeBaseCommitId, RebaseCommitId, MigrationMode.Upgrade);
            if (upstreamBranchMigrators.Any())
            {
                throw new NotSupportedException("Rebase is not supported when the branch being merged contains any migrator.");
            }
        }

        private void UpdateUpstreamBranchModifiedPaths(IRepository repository, Commit rebaseCommit, Commit mergeBaseCommit)
        {
            using (var changes = repository.Diff.Compare<TreeChanges>(mergeBaseCommit.Tree, rebaseCommit.Tree))
            {
                ModifiedUpstreamBranchEntries = changes.ToImmutableList();
            }
        }

        /// <inheritdoc />
        public IObjectRepositoryRebase Continue()
        {
            Status = Repository.Execute(repository =>
            {
                EnsureHeadCommit(repository);
                return _rebaseProcessorFactory(this).Continue(repository);
            });
            return this;
        }

        /// <summary>
        /// Clears the changes.
        /// </summary>
        internal void ClearChanges()
        {
            ModifiedProperties.Clear();
            AddedObjects.Clear();
            DeletedObjects.Clear();
        }
    }
}