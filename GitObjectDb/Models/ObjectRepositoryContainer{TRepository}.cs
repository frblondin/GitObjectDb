using FluentValidation.Results;
using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.Models.Compare;
using GitObjectDb.Services;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models
{
    /// <inheritdoc />
    [DebuggerDisplay("Path = {Path}, Repositories = {Repositories.Count}")]
    public class ObjectRepositoryContainer<TRepository> : ObjectRepositoryContainer, IObjectRepositoryContainer<TRepository>
        where TRepository : AbstractObjectRepository
    {
        readonly Func<IObjectRepositoryContainer, RepositoryDescription, IComputeTreeChanges> _computeTreeChangesFactory;
        readonly Func<IObjectRepositoryContainer, RepositoryDescription, IObjectRepository, string, IMetadataTreeMerge> _metadataTreeMergeFactory;
        readonly IObjectRepositoryLoader _repositoryLoader;
        readonly IRepositoryProvider _repositoryProvider;
        readonly GitHooks _hooks;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryContainer{TRepository}"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="path">The path.</param>
        public ObjectRepositoryContainer(IServiceProvider serviceProvider, string path)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _repositoryLoader = serviceProvider.GetRequiredService<IObjectRepositoryLoader>();
            _computeTreeChangesFactory = serviceProvider.GetRequiredService<Func<IObjectRepositoryContainer, RepositoryDescription, IComputeTreeChanges>>();
            _metadataTreeMergeFactory = serviceProvider.GetRequiredService<Func<IObjectRepositoryContainer, RepositoryDescription, IObjectRepository, string, IMetadataTreeMerge>>();
            _repositoryProvider = serviceProvider.GetRequiredService<IRepositoryProvider>();
            _hooks = serviceProvider.GetRequiredService<GitHooks>();

            Path = path ?? throw new ArgumentNullException(nameof(path));
            Directory.CreateDirectory(path);

            Repositories = LoadRepositories();
        }

        /// <inheritdoc />
        public override string Path { get; }

        /// <inheritdoc />
        public new IImmutableSet<TRepository> Repositories { get; private set; }

        /// <inheritdoc />
        public TRepository this[Guid id] =>
            GetRepository(id);

        TRepository GetRepository(Guid id) =>
            Repositories.FirstOrDefault(r => r.Id == id) ??
            throw new ObjectNotFoundException("The repository could not be found.");

        /// <inheritdoc />
        protected override IEnumerable<IObjectRepository> GetRepositoriesCore() => Repositories;

        IImmutableSet<TRepository> LoadRepositories()
        {
            var builder = ImmutableSortedSet.CreateBuilder(MetadataObjectIdComparer<TRepository>.Instance);
            foreach (var repositoryPath in Directory.EnumerateDirectories(Path))
            {
                if (Repository.IsValid(repositoryPath))
                {
                    var description = new RepositoryDescription(repositoryPath);
                    builder.Add(_repositoryLoader.LoadFrom(this, description));
                }
            }
            return builder.ToImmutable();
        }

        /// <inheritdoc />
        public override IObjectRepository TryGetRepository(Guid id) =>
            Repositories.FirstOrDefault(r => r.Id == id);

        /// <inheritdoc />
        public TRepository Clone(string repository, ObjectId commitId = null, OdbBackend backend = null)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            // Clone & load in a temp folder to extract the repository id
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            var tempRepoDescription = new RepositoryDescription(tempPath, backend);
            var cloned = _repositoryLoader.Clone(this, repository, tempRepoDescription, commitId);
            _repositoryProvider.Evict(tempRepoDescription);

            var path = System.IO.Path.Combine(Path, cloned.Id.ToString());
            Directory.Move(tempPath, path);
            var repositoryDescription = new RepositoryDescription(path, backend);
            return ReloadRepository(repositoryDescription, cloned.CommitId);
        }

        /// <inheritdoc />
        public TRepository AddRepository(TRepository repository, Signature signature, string message, OdbBackend backend = null, bool isBare = false)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var repositoryDescription = new RepositoryDescription(System.IO.Path.Combine(Path, repository.Id.ToString()), backend);
            EnsureNewRepository(repository, repositoryDescription);
            LibGit2Sharp.Repository.Init(repositoryDescription.Path, isBare);

            return repository.RepositoryProvider.Execute(repositoryDescription, r =>
            {
                var all = repository.Flatten().Select(o => new MetadataTreeEntryChanges(o.GetDataPath(), ChangeKind.Added, @new: o));
                var changes = new MetadataTreeChanges(repository, all.ToImmutableList());
                var commit = r.CommitChanges(changes, message, signature, signature, _hooks);
                if (commit == null)
                {
                    return null;
                }
                return ReloadRepository(repositoryDescription, commit.Id);
            });
        }

        void EnsureNewRepository(IObjectRepository repository, RepositoryDescription repositoryDescription)
        {
            if (Repositories.Any(r => r.Id == repository.Id))
            {
                throw new GitObjectDbException($"A repository with the same id already exists in the container repositories.");
            }
            if (Directory.Exists(repositoryDescription.Path))
            {
                throw new GitObjectDbException($"A repository with the target path already exists on the filesystem.");
            }
        }

        /// <inheritdoc />
        public TRepository Commit(IObjectRepository repository, Signature signature, string message, CommitOptions options = null)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            var previousRepository = Repositories.FirstOrDefault(r => r.Id == repository.Id) ??
                throw new NotImplementedException("The repository to update could not be found.");

            var repositoryDescription = previousRepository.RepositoryDescription;
            return previousRepository.RepositoryProvider.Execute(repositoryDescription, r =>
            {
                if (!r.Head.Tip.Id.Equals(previousRepository.CommitId))
                {
                    throw new GitObjectDbException("The current head commit id is different from the commit used by current instance.");
                }

                var computeChanges = _computeTreeChangesFactory(this, repositoryDescription);
                var changes = computeChanges.Compare(previousRepository, repository);
                if (changes.Any())
                {
                    var commit = r.CommitChanges(changes, message, signature, signature, _hooks, options).Id;
                    return (TRepository)ReloadRepository(previousRepository, commit);
                }
                else
                {
                    return previousRepository;
                }
            });
        }

        /// <inheritdoc />
        internal override IObjectRepository ReloadRepository(IObjectRepository previousRepository, ObjectId commit) =>
            ReloadRepository(previousRepository.RepositoryDescription, commit);

        TRepository ReloadRepository(RepositoryDescription repositoryDescription, ObjectId commit)
        {
            var result = _repositoryLoader.LoadFrom(this, repositoryDescription, commit);
            return AddOrReplace(result);
        }

        TRepository AddOrReplace(TRepository newRepository)
        {
            if (Repositories.TryGetValue(newRepository, out var old))
            {
                Repositories = Repositories.Remove(old).Add(newRepository);
            }
            else
            {
                Repositories = Repositories.Add(newRepository);
            }
            return newRepository;
        }

        /// <inheritdoc />
        public TRepository Checkout(TRepository repository, string branchName)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            AssertCurrentRepository(repository);
            return repository.RepositoryProvider.Execute(repository.RepositoryDescription, r =>
            {
                var branch = r.Branches[branchName];
                r.Refs.MoveHeadTarget(branch.CanonicalName);

                var newRepository = _repositoryLoader.LoadFrom(this, repository.RepositoryDescription, branch.Tip.Id);
                return AddOrReplace(newRepository);
            });
        }

        /// <inheritdoc />
        public TRepository Checkout(Guid id, string branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            return Checkout(this[id], branchName);
        }

        /// <inheritdoc />
        public TRepository Branch(TRepository repository, string branchName)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            AssertCurrentRepository(repository);
            return repository.RepositoryProvider.Execute(repository.RepositoryDescription, r =>
            {
                var branch = r.CreateBranch(branchName);
                r.Refs.MoveHeadTarget(branch.CanonicalName);

                var newRepository = _repositoryLoader.LoadFrom(this, repository.RepositoryDescription, branch.Tip.Id);
                return AddOrReplace(newRepository);
            });
        }

        /// <inheritdoc />
        public TRepository Branch(Guid id, string branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            return Branch(this[id], branchName);
        }

        /// <inheritdoc />
        public IMetadataTreeMerge Merge(TRepository repository, string branchName)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            AssertCurrentRepository(repository);
            return _metadataTreeMergeFactory(this, repository.RepositoryDescription, repository, branchName);
        }

        /// <inheritdoc />
        public IMetadataTreeMerge Merge(Guid id, string branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            return Merge(this[id], branchName);
        }

        void AssertCurrentRepository(IObjectRepository repository)
        {
            if (!Repositories.Contains(repository))
            {
                if (Repositories.Any(r => r.Id == repository.Id))
                {
                    throw new GitObjectDbException("The repository version is not currently managed by the container. This likely means that the repository was modified (commit, branch checkout...).");
                }
                throw new GitObjectDbException("The repository is not currently managed by the container.");
            }
        }

        /// <inheritdoc />
        public override ValidationResult Validate(ValidationRules rules = ValidationRules.All) =>
            new ValidationResult(Repositories.SelectMany(r => r.Validate(rules).Errors));
    }
}
