using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Models;
using LibGit2Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GitObjectDb.Migrations
{
    /// <summary>
    /// Scaffolds migrations to apply pending model changes.
    /// </summary>
    public class MigrationScaffolder
    {
        readonly JsonSerializer _serializer;
        readonly IRepositoryProvider _repositoryProvider;
        readonly RepositoryDescription _repositoryDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationScaffolder"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <exception cref="ArgumentNullException">
        /// serviceProvider
        /// or
        /// repository
        /// </exception>
        public MigrationScaffolder(IServiceProvider serviceProvider, RepositoryDescription repositoryDescription)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _serializer = serviceProvider.GetRequiredService<IObjectRepositoryLoader>().GetJsonSerializer();
            _repositoryProvider = serviceProvider.GetRequiredService<IRepositoryProvider>();
            _repositoryDescription = repositoryDescription ?? throw new ArgumentNullException(nameof(repositoryDescription));
        }

        /// <summary>
        /// Scaffolds a code based migration to apply any pending model changes to the database.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>The <see cref="Migrator"/> used to apply migrations.</returns>
        public IImmutableList<Migrator> Scaffold(ObjectId start, ObjectId end, MigrationMode mode)
        {
            if (mode == MigrationMode.Downgrade)
            {
                throw new NotImplementedException(MigrationMode.Downgrade.ToString());
            }

            return _repositoryProvider.Execute(_repositoryDescription, repository =>
            {
                var log = repository.Commits.QueryBy(new CommitFilter
                {
                    SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
                    ExcludeReachableFrom = start,
                    IncludeReachableFrom = end
                });
                var deferred = new List<IMigration>();
                var result = ImmutableList.CreateBuilder<Migrator>();
                result.AddRange(GetLogMigrators(repository, log, deferred, repository.Lookup<Commit>(start), mode));
                if (deferred.Any())
                {
                    var uniqueDeferredMigrations = deferred.Distinct(MetadataObjectIdComparer<IMigration>.Instance);
                    if (result.Any())
                    {
                        var toUpdate = result[result.Count - 1];
                        var newValue = new Migrator(toUpdate.Migrations.Concat(deferred).ToImmutableList(), mode, toUpdate.CommitId);
                        result[result.Count - 1] = newValue;
                    }
                    else
                    {
                        result.Add(new Migrator(uniqueDeferredMigrations.ToImmutableList(), mode, end));
                    }
                }
                return result.ToImmutable();
            });
        }

        IEnumerable<Migrator> GetLogMigrators(IRepository repository, ICommitLog log, List<IMigration> deferred, Commit previousCommit, MigrationMode mode)
        {
            foreach (var commit in log)
            {
                if (previousCommit != null)
                {
                    var migrations = GetCommitMigrations(repository, previousCommit, commit).ToList();

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

        IEnumerable<IMigration> GetCommitMigrations(IRepository repository, Commit previousCommit, Commit commit)
        {
            using (var changes = repository.Diff.Compare<TreeChanges>(previousCommit.Tree, commit.Tree))
            {
                foreach (var change in changes.Where(c => c.Path.StartsWith(AbstractObjectRepository.MigrationFolder, StringComparison.OrdinalIgnoreCase) && (c.Status == ChangeKind.Added || c.Status == ChangeKind.Modified)))
                {
                    var blob = (Blob)commit[change.Path].Target;
                    var jobject = blob.GetContentStream().ToJson<JObject>(_serializer);
                    var objectType = Type.GetType(jobject.Value<string>("$type"));
                    yield return (IMigration)jobject.ToObject(objectType, _serializer);
                }
            }
        }
    }
}
