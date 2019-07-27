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

namespace GitObjectDb.Models.CherryPick
{
    /// <inheritdoc />
    [ExcludeFromGuardForNull]
    internal class ObjectRepositoryCherryPick : IObjectRepositoryCherryPick
    {
        private readonly MigrationScaffolderFactory _migrationScaffolderFactory;
        private readonly CherryPickProcessor.Factory _cherryPickProcessorFactory;

        [ActivatorUtilitiesConstructor]
        public ObjectRepositoryCherryPick(IObjectRepository repository, ObjectId cherryPickCommitId,
            MigrationScaffolderFactory migrationScaffolderFactory, CherryPickProcessor.Factory cherryPickProcessorFactory)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            HeadCommitId = repository.CommitId ?? throw new GitObjectDbException("Repository instance is not linked to any commit.");
            CherryPickCommitId = cherryPickCommitId ?? throw new ArgumentNullException(nameof(cherryPickCommitId));

            _migrationScaffolderFactory = migrationScaffolderFactory ?? throw new ArgumentNullException(nameof(migrationScaffolderFactory));
            _cherryPickProcessorFactory = cherryPickProcessorFactory ?? throw new ArgumentNullException(nameof(cherryPickProcessorFactory));

            Initialize();
        }

        /// <summary>
        /// Gets the repository.
        /// </summary>
        public IObjectRepository Repository { get; }

        /// <inheritdoc/>
        public ObjectId HeadCommitId { get; }

        /// <inheritdoc/>
        public ObjectId CherryPickCommitId { get; private set; }

        /// <inheritdoc/>
        public CherryPickStatus Status { get; private set; }

        /// <inheritdoc/>
        public IList<ObjectRepositoryPropertyChange> ModifiedProperties { get; } = new List<ObjectRepositoryPropertyChange>();

        /// <inheritdoc/>
        public IList<ObjectRepositoryAdd> AddedObjects { get; } = new List<ObjectRepositoryAdd>();

        /// <inheritdoc/>
        public IList<ObjectRepositoryDelete> DeletedObjects { get; } = new List<ObjectRepositoryDelete>();

        /// <inheritdoc/>
        public IObjectRepository Result { get; private set; }

        /// <summary>
        /// Gets the modified upstream branch entries.
        /// </summary>
        internal IImmutableList<TreeEntryChanges> ModifiedUpstreamBranchEntries { get; private set; }

        private void Initialize()
        {
            Repository.Execute(repository =>
            {
                EnsureHeadCommit(repository);

                var cherryPickCommit = repository.Lookup<Commit>(CherryPickCommitId);
                var parentCommits = cherryPickCommit.Parents.ToList();
                if (parentCommits.Count > 1)
                {
                    throw new NotSupportedException("Commit has more than one parent.");
                }
                var parentCommit = parentCommits.Single();

                EnsureNoMigrations(parentCommit);
                UpdateUpstreamBranchModifiedPaths(repository, cherryPickCommit, parentCommit);

                (Status, Result) = _cherryPickProcessorFactory(this).Initialize(repository, parentCommit);
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

        private void EnsureNoMigrations(Commit parentCommit)
        {
            var migrationScaffolder = _migrationScaffolderFactory(Repository.Container, Repository.RepositoryDescription);
            var migrators = migrationScaffolder.Scaffold(parentCommit.Id, CherryPickCommitId, MigrationMode.Upgrade);
            if (migrators.Any())
            {
                throw new NotSupportedException("Cherry pick is not supported when the commit containts any migrator.");
            }
        }

        private void UpdateUpstreamBranchModifiedPaths(IRepository repository, Commit cherryPickCommit, Commit parentCommit)
        {
            using (var changes = repository.Diff.Compare<TreeChanges>(parentCommit.Tree, cherryPickCommit.Tree))
            {
                ModifiedUpstreamBranchEntries = changes.ToImmutableList();
            }
        }

        /// <inheritdoc />
        public IObjectRepository Commit()
        {
            (Status, Result) = Repository.Execute(repository =>
                {
                    EnsureHeadCommit(repository);
                    return _cherryPickProcessorFactory(this).Complete(repository);
                });
            return Result;
        }
    }
}