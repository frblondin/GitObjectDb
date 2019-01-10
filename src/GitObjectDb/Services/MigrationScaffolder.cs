using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Models.Migration;
using GitObjectDb.Services;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using GitObjectDb.Serialization;

namespace GitObjectDb.Services
{
    /// <inheritdoc/>
    public class MigrationScaffolder : IMigrationScaffolder
    {
        private readonly IRepositoryProvider _repositoryProvider;
        private readonly IObjectRepositoryContainer _container;
        private readonly RepositoryDescription _repositoryDescription;
        private readonly ObjectRepositorySerializerFactory _repositorySerializerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationScaffolder"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="repositoryProvider">The repository provider.</param>
        /// <param name="repositorySerializerFactory">The <see cref="IObjectRepositorySerializer"/> factory.</param>
        [ActivatorUtilitiesConstructor]
        internal MigrationScaffolder(IObjectRepositoryContainer container, RepositoryDescription repositoryDescription,
            IRepositoryProvider repositoryProvider, ObjectRepositorySerializerFactory repositorySerializerFactory)
        {
            _repositoryProvider = repositoryProvider ?? throw new ArgumentNullException(nameof(repositoryProvider));
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _repositoryDescription = repositoryDescription ?? throw new ArgumentNullException(nameof(repositoryDescription));
            _repositorySerializerFactory = repositorySerializerFactory ?? throw new ArgumentNullException(nameof(repositorySerializerFactory));
        }

        /// <inheritdoc/>
        public IImmutableList<Migrator> Scaffold(ObjectId migrationStart, ObjectId migrationEnd, MigrationMode mode)
        {
            if (migrationStart == null)
            {
                throw new ArgumentNullException(nameof(migrationStart));
            }
            if (migrationEnd == null)
            {
                throw new ArgumentNullException(nameof(migrationEnd));
            }

            if (mode == MigrationMode.Downgrade)
            {
                throw new NotImplementedException(MigrationMode.Downgrade.ToString());
            }

            return _repositoryProvider.Execute(_repositoryDescription, repository =>
            {
                var log = repository.Commits.QueryBy(new CommitFilter
                {
                    SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
                    ExcludeReachableFrom = migrationStart,
                    IncludeReachableFrom = migrationEnd
                });
                var deferred = new List<IMigration>();
                var result = ImmutableList.CreateBuilder<Migrator>();
                result.AddRange(GetLogMigrators(repository, log, deferred, repository.Lookup<Commit>(migrationStart), mode));
                if (deferred.Any())
                {
                    var uniqueDeferredMigrations = deferred.Distinct(ObjectRepositoryIdComparer<IMigration>.Instance);
                    if (result.Any())
                    {
                        var toUpdate = result[result.Count - 1];
                        var newValue = new Migrator(toUpdate.Migrations.Concat(deferred).ToImmutableList(), mode, toUpdate.CommitId);
                        result[result.Count - 1] = newValue;
                    }
                    else
                    {
                        result.Add(new Migrator(uniqueDeferredMigrations.ToImmutableList(), mode, migrationEnd));
                    }
                }
                return result.ToImmutable();
            });
        }

        private IEnumerable<Migrator> GetLogMigrators(IRepository repository, ICommitLog log, List<IMigration> deferred, Commit previousCommit, MigrationMode mode)
        {
            var context = new ModelObjectSerializationContext(_container);
            var serializer = _repositorySerializerFactory(context);
            foreach (var commit in log)
            {
                if (previousCommit != null)
                {
                    var migrations = GetCommitMigrations(repository, previousCommit, commit, serializer).ToList();

                    deferred.AddRange(migrations.Where(m => m.IsIdempotent));

                    migrations.RemoveAll(m => m.IsIdempotent);
                    if (migrations.Any())
                    {
                        yield return new Migrator(migrations.Where(m => !m.IsIdempotent).ToImmutableList(), mode, commit.Id);
                    }
                }
                previousCommit = commit;
            }
        }

        private static IEnumerable<IMigration> GetCommitMigrations(IRepository repository, Commit previousCommit, Commit commit, IObjectRepositorySerializer serializer)
        {
            using (var changes = repository.Diff.Compare<TreeChanges>(previousCommit.Tree, commit.Tree))
            {
                foreach (var change in changes.Where(c => c.Path.StartsWith(FileSystemStorage.MigrationFolder, StringComparison.OrdinalIgnoreCase) && (c.Status == ChangeKind.Added || c.Status == ChangeKind.Modified)))
                {
                    var blob = (Blob)commit[change.Path].Target;
                    yield return (IMigration)serializer.Deserialize(blob.GetContentStream());
                }
            }
        }
    }
}
