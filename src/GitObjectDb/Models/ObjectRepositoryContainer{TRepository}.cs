using GitObjectDb.Attributes;
using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.Models.Compare;
using GitObjectDb.Models.Merge;
using GitObjectDb.Models.Rebase;
using GitObjectDb.Services;
using GitObjectDb.Validations;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        where TRepository : class, IObjectRepository
    {
        private readonly IObjectRepositoryLoader _repositoryLoader;
        private readonly ComputeTreeChangesFactory _computeTreeChangesFactory;
        private readonly ObjectRepositoryMergeFactory _objectRepositoryMergeFactory;
        private readonly ObjectRepositoryRebaseFactory _objectRepositoryRebaseFactory;
        private readonly IRepositoryProvider _repositoryProvider;
        private readonly GitHooks _hooks;
        private readonly ILogger<ObjectRepositoryContainer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryContainer{TRepository}"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="repositoryLoader">The repository loader.</param>
        /// <param name="computeTreeChangesFactory">The <see cref="IComputeTreeChanges"/> factory.</param>
        /// <param name="objectRepositoryMergeFactory">The <see cref="IObjectRepositoryMerge"/> factory.</param>
        /// <param name="objectRepositoryRebaseFactory">The <see cref="IObjectRepositoryRebase"/> factory.</param>
        /// <param name="repositoryProvider">The repository provider.</param>
        /// <param name="hooks">The hooks.</param>
        /// <param name="logger">The logger.</param>
        public ObjectRepositoryContainer(string path,
            IObjectRepositoryLoader repositoryLoader, ComputeTreeChangesFactory computeTreeChangesFactory,
            ObjectRepositoryMergeFactory objectRepositoryMergeFactory, ObjectRepositoryRebaseFactory objectRepositoryRebaseFactory,
            IRepositoryProvider repositoryProvider, GitHooks hooks, ILogger<ObjectRepositoryContainer> logger)
        {
            _repositoryLoader = repositoryLoader ?? throw new ArgumentNullException(nameof(repositoryLoader));
            _computeTreeChangesFactory = computeTreeChangesFactory ?? throw new ArgumentNullException(nameof(computeTreeChangesFactory));
            _objectRepositoryMergeFactory = objectRepositoryMergeFactory ?? throw new ArgumentNullException(nameof(objectRepositoryMergeFactory));
            _objectRepositoryRebaseFactory = objectRepositoryRebaseFactory ?? throw new ArgumentNullException(nameof(objectRepositoryRebaseFactory));
            _repositoryProvider = repositoryProvider ?? throw new ArgumentNullException(nameof(repositoryProvider));
            _hooks = hooks ?? throw new ArgumentNullException(nameof(hooks));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Path = path ?? throw new ArgumentNullException(nameof(path));
            Directory.CreateDirectory(path);

            Repositories = LoadRepositories();

            _logger.ContainerCreated(path);
        }

        /// <inheritdoc />
        public override string Path { get; }

        /// <inheritdoc />
        public new IImmutableSet<TRepository> Repositories { get; private set; }

        /// <inheritdoc />
        public TRepository this[UniqueId id] =>
            GetRepository(id);

        TRepository GetRepository(UniqueId id) =>
            Repositories.FirstOrDefault(r => r.Id == id) ??
            throw new ObjectNotFoundException("The repository could not be found.");

        /// <inheritdoc />
        protected override IEnumerable<IObjectRepository> GetRepositoriesCore() => Repositories;

        private IImmutableSet<TRepository> LoadRepositories()
        {
            var builder = ImmutableSortedSet.CreateBuilder(ObjectRepositoryIdComparer<TRepository>.Instance);
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
        public override IObjectRepository TryGetRepository(UniqueId id) =>
            Repositories.FirstOrDefault(r => r.Id == id);

        /// <inheritdoc />
        public TRepository Clone(string repository, ObjectId commitId = null, Func<OdbBackend> backend = null)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            using (_logger.BeginScope("Cloning repository '{Repository}' from commit id '{CommitId}'.", repository, commitId))
            {
                // Clone & load in a temp folder to extract the repository id
                var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), UniqueId.CreateNew().ToString());
                var tempRepoDescription = new RepositoryDescription(tempPath, backend);
                var cloned = _repositoryLoader.Clone(this, repository, tempRepoDescription, commitId);

                // Evict temp repository from repo provider
                _repositoryProvider.Evict(tempRepoDescription);

                // Move to target path
                var path = System.IO.Path.Combine(Path, cloned.Id.ToString());
                Directory.Move(tempPath, path);

                var repositoryDescription = new RepositoryDescription(path, backend);
                return ReloadRepository(repositoryDescription, cloned.CommitId);
            }
        }

        /// <inheritdoc />
        public TRepository AddRepository(TRepository repository, Signature signature, string message, Func<OdbBackend> backend = null, bool isBare = false)
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

            using (_logger.BeginScope("Adding repository '{Repository}'.", repository.Id))
            {
                var repositoryDescription = new RepositoryDescription(System.IO.Path.Combine(Path, repository.Id.ToString()), backend);
                EnsureNewRepository(repository, repositoryDescription);
                LibGit2Sharp.Repository.Init(repositoryDescription.Path, isBare);

                return _repositoryProvider.Execute(repositoryDescription, r =>
                {
                    var all = repository.Flatten().Select(o => new ObjectRepositoryEntryChanges(o.GetDataPath(), ChangeKind.Added, @new: o));
                    var changes = new ObjectRepositoryChanges(repository, all.ToImmutableList());
                    var commit = r.CommitChanges(changes, message, signature, signature, _hooks);
                    if (commit == null)
                    {
                        return null;
                    }
                    return ReloadRepository(repositoryDescription, commit.Id);
                });
            }
        }

        private void EnsureNewRepository(IObjectRepository repository, RepositoryDescription repositoryDescription)
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

            using (_logger.BeginScope("Committing changes to repository '{Repository}' using message '{Message}'.", repository.Id, message))
            {
                var previousRepository = Repositories.FirstOrDefault(r => r.Id == repository.Id) ??
                    throw new NotImplementedException("The repository to update could not be found.");

                var repositoryDescription = previousRepository.RepositoryDescription;
                return previousRepository.Execute(r =>
                {
                    EnsureHeadCommit(r, previousRepository);

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
        }

        /// <inheritdoc />
        public void Push(UniqueId id, string remoteName = null, PushOptions options = null)
        {
            if (options == null)
            {
                options = new PushOptions();
            }

            var repository = this[id];
            using (_logger.BeginScope("Pushing repository '{Repository}' changes.", repository.Id))
            {
                repository.Execute(r =>
                {
                    EnsureHeadCommit(r, repository);

                    if (string.IsNullOrEmpty(remoteName))
                    {
                        if (!r.Head.IsTracking)
                        {
                            throw new GitObjectDbException($"No remote name has been provided and the current branch is not linked to any remote.");
                        }
                        remoteName = r.Head.RemoteName;
                    }
                    var remote = r.Network.Remotes[remoteName];
                    r.Network.Push(remote, r.Head.CanonicalName, options);

                    SetRemoteBranchIfRequired(remoteName, r);
                });
            }
        }

        private static void SetRemoteBranchIfRequired(string remoteName, IRepository r)
        {
            if (!r.Head.IsTracking)
            {
                var currentBranch = r.Head;
                r.Branches.Update(currentBranch,
                    b => b.Remote = remoteName,
                    b => b.UpstreamBranch = currentBranch.CanonicalName);
            }
        }

        /// <inheritdoc />
        internal override IObjectRepository ReloadRepository(IObjectRepository previousRepository, ObjectId commit = null) =>
            ReloadRepository(previousRepository.RepositoryDescription, commit);

        private TRepository ReloadRepository(RepositoryDescription repositoryDescription, ObjectId commit = null)
        {
            var result = _repositoryLoader.LoadFrom(this, repositoryDescription, commit);
            return AddOrReplace(result);
        }

        private TRepository AddOrReplace(TRepository newRepository)
        {
            if (Repositories.TryGetValue(newRepository, out var old))
            {
                Repositories = Repositories.Remove(old).Add(newRepository);
                _logger.RepositoryUpdated(newRepository.Id, newRepository.CommitId);
            }
            else
            {
                Repositories = Repositories.Add(newRepository);
                _logger.RepositoryAdded(newRepository.Id, newRepository.CommitId);
            }
            return newRepository;
        }

        /// <inheritdoc />
        public TRepository Checkout(UniqueId id, string branchName, bool createNewBranch = false, string committish = null)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            var repository = this[id];
            using (_logger.BeginScope("Checking out repository '{Repository}' at '{BranchName}'.", repository.Id, branchName))
            {
                return repository.Execute(r =>
                {
                    var head = r.Head;
                    var branch = r.Branches[branchName];
                    if (createNewBranch)
                    {
                        if (branch != null)
                        {
                            throw new GitObjectDbException($"The branch '{branchName}' already exists.");
                        }
                        var reflogName = committish ?? (r.Refs.Head is SymbolicReference ? head.FriendlyName : head.Tip.Sha);
                        branch = r.CreateBranch(branchName, reflogName);
                    }
                    else if (branch == null)
                    {
                        throw new GitObjectDbException($"The branch '{branchName}' does not exist.");
                    }

                    r.Refs.MoveHeadTarget(branch.CanonicalName);

                    var newRepository = _repositoryLoader.LoadFrom(this, repository.RepositoryDescription, branch.Tip.Id);
                    return AddOrReplace(newRepository);
                });
            }
        }

        /// <inheritdoc />
        public TRepository Fetch(UniqueId id, FetchOptions options = null)
        {
            if (options == null)
            {
                options = new FetchOptions();
            }

            var repository = this[id];
            using (_logger.BeginScope("Fetching repository '{Repository}'.", repository.Id))
            {
                return repository.Execute(r =>
                {
                    var concrete = r as Repository ??
                        throw new NotSupportedException($"Object of type {nameof(Repository)} expected.");
                    Commands.Fetch(concrete, r.Head.RemoteName, Array.Empty<string>(), options, null);

                    return _repositoryLoader.LoadFrom(this, repository.RepositoryDescription, r.Head.TrackedBranch.Tip.Id);
                });
            }
        }

        /// <inheritdoc />
        public void FetchAll(UniqueId id, FetchOptions options = null)
        {
            if (options == null)
            {
                options = new FetchOptions();
            }

            var repository = this[id];
            using (_logger.BeginScope("Fetching all remotes for repository '{Repository}'.", repository.Id))
            {
                repository.RepositoryProvider.Execute(repository.RepositoryDescription, r =>
                {
                    var concrete = r as Repository ??
                        throw new NotSupportedException($"Object of type {nameof(Repository)} expected.");
                    foreach (var remote in r.Network.Remotes)
                    {
                        var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                        Commands.Fetch(concrete, remote.Name, refSpecs, options, null);
                    }
                });
            }
        }

        /// <inheritdoc />
        public IObjectRepositoryMerge Pull(UniqueId id, FetchOptions options = null)
        {
            if (options == null)
            {
                options = new FetchOptions();
            }

            var repository = this[id];
            using (_logger.BeginScope("Pulling repository '{Repository}'.", repository.Id))
            {
                var (originTip, remoteBranch) = repository.Execute(r =>
                {
                    var concrete = r as Repository ??
                        throw new NotSupportedException($"Object of type {nameof(Repository)} expected.");
                    Commands.Fetch(concrete, r.Head.RemoteName, Array.Empty<string>(), options, null);

                    return (r.Head.TrackedBranch.Tip.Id, r.Head.TrackedBranch.FriendlyName);
                });
                return _objectRepositoryMergeFactory(repository, originTip, remoteBranch);
            }
        }

        /// <inheritdoc />
        public IObjectRepositoryMerge Merge(UniqueId id, string branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            var repository = this[id];
            using (_logger.BeginScope("Merging repository '{Repository}' with '{BranchName}'.", repository.Id, branchName))
            {
                var commitId = repository.Execute(r => r.Branches[branchName].Tip.Id);
                return _objectRepositoryMergeFactory(repository, commitId, branchName);
            }
        }

        /// <inheritdoc />
        public IObjectRepositoryRebase Rebase(UniqueId id, string branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            var repository = this[id];
            using (_logger.BeginScope("Merging repository '{Repository}' with '{BranchName}'.", repository.Id, branchName))
            {
                var commitId = repository.Execute(r => r.Branches[branchName].Tip.Id);
                return _objectRepositoryRebaseFactory(repository, commitId, branchName);
            }
        }

        /// <inheritdoc />
        public override ValidationResult Validate(ValidationRules rules = ValidationRules.All) =>
            new ValidationResult(Repositories.SelectMany(r => r.Validate(rules).Errors).ToList());
    }
}
