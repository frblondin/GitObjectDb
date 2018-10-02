using GitObjectDb.Attributes;
using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.IO;
using GitObjectDb.Models.Compare;
using GitObjectDb.Models.Migration;
using GitObjectDb.Reflection;
using GitObjectDb.Services;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public class ObjectRepositoryRebase : IObjectRepositoryRebase
    {
        readonly IServiceProvider _serviceProvider;
        readonly IRepositoryProvider _repositoryProvider;
        readonly IObjectRepositoryLoader _repositoryLoader;
        readonly MigrationScaffolderFactory _migrationScaffolderFactory;

        readonly RepositoryDescription _repositoryDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryRebase"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="container">The container.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="repository">The repository on which to apply the merge.</param>
        /// <param name="rebaseCommitId">The commit to be rebased.</param>
        /// <param name="branchName">Name of the branch.</param>
        /// <exception cref="ArgumentNullException">
        /// serviceProvider
        /// or
        /// repositoryDescription
        /// or
        /// commitId
        /// or
        /// branchName
        /// or
        /// merger
        /// </exception>
        [ActivatorUtilitiesConstructor]
        public ObjectRepositoryRebase(IServiceProvider serviceProvider, IObjectRepositoryContainer container, RepositoryDescription repositoryDescription, IObjectRepository repository, ObjectId rebaseCommitId, string branchName)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            Container = container ?? throw new ArgumentNullException(nameof(container));
            _repositoryDescription = repositoryDescription ?? throw new ArgumentNullException(nameof(repositoryDescription));
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            HeadCommitId = repository.CommitId ?? throw new GitObjectDbException("Repository instance is not linked to any commit.");
            RebaseCommitId = rebaseCommitId ?? throw new ArgumentNullException(nameof(rebaseCommitId));
            BranchName = branchName ?? throw new ArgumentNullException(nameof(branchName));

            _repositoryProvider = serviceProvider.GetRequiredService<IRepositoryProvider>();
            _repositoryLoader = serviceProvider.GetRequiredService<IObjectRepositoryLoader>();
            _migrationScaffolderFactory = serviceProvider.GetRequiredService<MigrationScaffolderFactory>();

            Initialize();
        }

        /// <summary>
        /// Gets the container.
        /// </summary>
        public IObjectRepositoryContainer Container { get; }

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
        public IList<ObjectRepositoryChunkChange> ModifiedChunks { get; } = new List<ObjectRepositoryChunkChange>();

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

        void Initialize()
        {
            _repositoryProvider.Execute(_repositoryDescription, repository =>
            {
                EnsureHeadCommit(repository);

                var rebaseCommit = repository.Lookup<Commit>(RebaseCommitId);
                var headTip = repository.Head.Tip;
                var mergeBaseCommit = repository.ObjectDatabase.FindMergeBase(headTip, rebaseCommit);
                MergeBaseCommitId = mergeBaseCommit.Id;

                EnsureNoMigrations(repository);
                ReplayedCommits = repository.Commits.QueryBy(new CommitFilter
                {
                    SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
                    ExcludeReachableFrom = rebaseCommit,
                    IncludeReachableFrom = headTip
                }).Select(c => c.Id).ToImmutableList();
                UpdateUpstreamBranchModifiedPaths(repository, rebaseCommit, mergeBaseCommit);

                StartRepository = _repositoryLoader.LoadFrom(Container, Repository.RepositoryDescription, RebaseCommitId);

                Status = new RebaseProcessor(_serviceProvider, this).ContinueNext(repository);
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

        void EnsureNoMigrations(IRepository repository)
        {
            var migrationScaffolder = _migrationScaffolderFactory(Container, Repository.RepositoryDescription);
            var upstreamBranchMigrators = migrationScaffolder.Scaffold(MergeBaseCommitId, RebaseCommitId, MigrationMode.Upgrade);
            var currentBranchMigrators = migrationScaffolder.Scaffold(MergeBaseCommitId, repository.Head.Tip.Id, MigrationMode.Upgrade);
            if (upstreamBranchMigrators.Any() || currentBranchMigrators.Any())
            {
                throw new NotSupportedException("Rebase is not supported when branches contain any migrator.");
            }
        }

        void UpdateUpstreamBranchModifiedPaths(IRepository repository, Commit rebaseCommit, Commit mergeBaseCommit)
        {
            using (var changes = repository.Diff.Compare<TreeChanges>(mergeBaseCommit.Tree, rebaseCommit.Tree))
            {
                ModifiedUpstreamBranchEntries = changes.ToImmutableList();
            }
        }

        /// <inheritdoc />
        public IObjectRepositoryRebase Continue()
        {
            Status = Repository.Execute(
                r => new RebaseProcessor(_serviceProvider, this).Continue(r));
            return this;
        }

        /// <summary>
        /// Clears the changes.
        /// </summary>
        internal void ClearChanges()
        {
            ModifiedChunks.Clear();
            AddedObjects.Clear();
            DeletedObjects.Clear();
        }
    }
}