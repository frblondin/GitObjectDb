using GitObjectDb.IO;
using GitObjectDb.Models.Migration;
using GitObjectDb.Services;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models.Rebase
{
    /// <inheritdoc />
    public class ObjectRepositoryRebase : IObjectRepositoryRebase
    {
        readonly MigrationScaffolderFactory _migrationScaffolderFactory;

        readonly IObjectRepository _repository;
        readonly Action _onCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryRebase"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="repository">The repository.</param>
        /// <param name="onCompleted">Delegate that will be invoked when the rebase operation has completed.</param>
        /// <exception cref="ArgumentNullException">repository</exception>
        [ActivatorUtilitiesConstructor]
        public ObjectRepositoryRebase(IServiceProvider serviceProvider, IObjectRepository repository, Action onCompleted)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _onCompleted = onCompleted ?? throw new ArgumentNullException(nameof(onCompleted));

            _migrationScaffolderFactory = serviceProvider.GetRequiredService<MigrationScaffolderFactory>();
        }

        /// <inheritdoc />
        public ObjectRepositoryRebaseResult Start(string upstreamBranchName, Identity committer, RebaseOptions options = null)
        {
            if (upstreamBranchName == null)
            {
                throw new ArgumentNullException(nameof(upstreamBranchName));
            }
            if (committer == null)
            {
                throw new ArgumentNullException(nameof(committer));
            }
            if (options == null)
            {
                options = new RebaseOptions();
            }

            _repository.EnsuresCurrentRepository();
            return _repository.Execute(r =>
            {
                var upstreamBranch = r.Branches[upstreamBranchName];
                EnsureNoMigrations(r, upstreamBranch);

                r.Reset(ResetMode.Hard);
                var result = r.Rebase.Start(null, upstreamBranch, null, committer, options);

                ProcessRebaseResult(r, result);

                return new ObjectRepositoryRebaseResult(result);
            });
        }

        void EnsureNoMigrations(IRepository repository, Branch upstreamBranch)
        {
            var baseCommit = repository.ObjectDatabase.FindMergeBase(repository.Head.Tip, upstreamBranch.Tip);
            var migrationScaffolder = _migrationScaffolderFactory(_repository.Container, _repository.RepositoryDescription);
            var upstreamBranchMigrators = migrationScaffolder.Scaffold(baseCommit.Id, upstreamBranch.Tip.Id, MigrationMode.Upgrade);
            var currentBranchMigrators = migrationScaffolder.Scaffold(baseCommit.Id, repository.Head.Tip.Id, MigrationMode.Upgrade);
            if (upstreamBranchMigrators.Any() || currentBranchMigrators.Any())
            {
                throw new NotSupportedException("Rebase is not supported when branches contain any migrator.");
            }
        }

        void ProcessRebaseResult(IRepository repository, RebaseResult result)
        {
            switch (result.Status)
            {
                case RebaseStatus.Complete:
                    _onCompleted();
                    RemoveFetchedFiles();
                    break;
                default:
                    repository.Rebase.Abort();
                    RemoveFetchedFiles();
                    throw new NotSupportedException($"The rebase start returned a {result.Status} status which is not yet supported.");
            }
        }

        void RemoveFetchedFiles()
        {
            var exclusions = new[] { Path.Combine(_repository.RepositoryDescription.Path, ".git") };
            DirectoryUtils.Delete(_repository.RepositoryDescription.Path, continueOnError: true, exclusions);
        }
    }
}
