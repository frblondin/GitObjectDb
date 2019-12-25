using GitObjectDb.Attributes;
using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.Models.CherryPick;
using GitObjectDb.Models.Compare;
using GitObjectDb.Models.Merge;
using GitObjectDb.Models.Rebase;
using GitObjectDb.Serialization;
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
using System.Threading.Tasks;

namespace GitObjectDb.Models
{
    /// <inheritdoc />
    [DebuggerDisplay("Path = {Path}, Repositories = {Repositories.Count}")]
    public class ObjectRepositoryContainer<TRepository> : ObjectRepositoryContainer, IObjectRepositoryContainer<TRepository>
        where TRepository : class, IObjectRepository
    {
        private readonly IObjectRepositoryLoader _repositoryLoader;
        private readonly ComputeTreeChangesFactory _computeTreeChangesFactory;
        private readonly ObjectRepositoryMergeFactoryAsync _objectRepositoryMergeFactory;
        private readonly ObjectRepositoryRebaseFactoryAsync _objectRepositoryRebaseFactory;
        private readonly ObjectRepositoryCherryPickFactoryAsync _objectRepositoryCherryPickFactory;
        private readonly IRepositoryProvider _repositoryProvider;
        private readonly GitHooks _hooks;
        private readonly ObjectRepositorySerializerFactory _serializerFactory;
        private readonly ILogger<ObjectRepositoryContainer> _logger;

        internal ObjectRepositoryContainer(string path,
            IObjectRepositoryLoader repositoryLoader, ComputeTreeChangesFactory computeTreeChangesFactory,
            ObjectRepositoryMergeFactoryAsync objectRepositoryMergeFactory, ObjectRepositoryRebaseFactoryAsync objectRepositoryRebaseFactory,
            ObjectRepositoryCherryPickFactoryAsync objectRepositoryCherryPickFactory,
            IRepositoryProvider repositoryProvider, GitHooks hooks,
            ObjectRepositorySerializerFactory serializerFactory, ILogger<ObjectRepositoryContainer> logger)
        {
            _repositoryLoader = repositoryLoader ?? throw new ArgumentNullException(nameof(repositoryLoader));
            _computeTreeChangesFactory = computeTreeChangesFactory ?? throw new ArgumentNullException(nameof(computeTreeChangesFactory));
            _objectRepositoryMergeFactory = objectRepositoryMergeFactory ?? throw new ArgumentNullException(nameof(objectRepositoryMergeFactory));
            _objectRepositoryRebaseFactory = objectRepositoryRebaseFactory ?? throw new ArgumentNullException(nameof(objectRepositoryRebaseFactory));
            _objectRepositoryCherryPickFactory = objectRepositoryCherryPickFactory ?? throw new ArgumentNullException(nameof(objectRepositoryCherryPickFactory));
            _repositoryProvider = repositoryProvider ?? throw new ArgumentNullException(nameof(repositoryProvider));
            _hooks = hooks ?? throw new ArgumentNullException(nameof(hooks));
            _serializerFactory = serializerFactory ?? throw new ArgumentNullException(nameof(serializerFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Path = path ?? throw new ArgumentNullException(nameof(path));
            Directory.CreateDirectory(path);

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

        internal async Task LoadRepositoriesAsync()
        {
            var builder = ImmutableSortedSet.CreateBuilder(ObjectRepositoryIdComparer<TRepository>.Instance);
            foreach (var repositoryPath in Directory.EnumerateDirectories(Path))
            {
                if (Repository.IsValid(repositoryPath))
                {
                    var description = new RepositoryDescription(repositoryPath);
                    builder.Add(await _repositoryLoader.LoadFromAsync(this, description).ConfigureAwait(false));
                }
            }
            Repositories = builder.ToImmutable();
        }

        /// <inheritdoc />
        public override IObjectRepository TryGetRepository(UniqueId id) =>
            Repositories.FirstOrDefault(r => r.Id == id);

        /// <inheritdoc />
        public async Task<TRepository> CloneAsync(string repository, ObjectId commitId = null, Func<OdbBackend> backend = null)
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
                var cloned = await _repositoryLoader.CloneAsync(this, repository, tempRepoDescription, commitId).ConfigureAwait(false);

                // Evict temp repository from repo provider
                _repositoryProvider.Evict(tempRepoDescription);

                // Move to target path
                var path = System.IO.Path.Combine(Path, cloned.Id.ToString());
                Directory.Move(tempPath, path);

                var repositoryDescription = new RepositoryDescription(path, backend);
                return await ReloadRepositoryAsync(repositoryDescription, cloned.CommitId).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<TRepository> AddRepositoryAsync(TRepository repository, Signature signature, string message, Func<OdbBackend> backend = null, bool isBare = false)
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

                return await _repositoryProvider.ExecuteAsync(repositoryDescription, async r =>
                {
                    var all = repository.FlattenAsync().Select(o => new ObjectRepositoryEntryChanges(o.GetDataPath(), ChangeKind.Added, @new: o));
                    var changes = new ObjectRepositoryChangeCollection(repository, all.ToImmutableList());
                    var commit = await r.CommitChangesAsync(changes, _serializerFactory(), message, signature, signature, _hooks).ConfigureAwait(false);
                    if (commit == null)
                    {
                        return null;
                    }
                    return await ReloadRepositoryAsync(repositoryDescription, commit.Id).ConfigureAwait(false);
                }).ConfigureAwait(false);
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
        public async Task<TRepository> CommitAsync(IObjectRepository repository, Signature signature, string message, CommitOptions options = null)
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
                return await previousRepository.ExecuteAsync(async r =>
                {
                    EnsureHeadCommit(r, previousRepository);

                    var computeChanges = _computeTreeChangesFactory(this, repositoryDescription);
                    var changes = computeChanges.Compare(previousRepository, repository);
                    if (changes.Any())
                    {
                        var commit = await r.CommitChangesAsync(changes, _serializerFactory(), message, signature, signature, _hooks, options).ConfigureAwait(false);
                        var commitId = commit.Id;
                        return (TRepository)await ReloadRepositoryAsync(previousRepository, commitId).ConfigureAwait(false);
                    }
                    else
                    {
                        return previousRepository;
                    }
                }).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task PushAsync(UniqueId id, string remoteName = null, PushOptions options = null)
        {
            if (options == null)
            {
                options = new PushOptions();
            }

            var repository = this[id];
            using (_logger.BeginScope("Pushing repository '{Repository}' changes.", repository.Id))
            {
                await repository.ExecuteAsync(r =>
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
                }).ConfigureAwait(false);
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
        internal override async Task<IObjectRepository> ReloadRepositoryAsync(IObjectRepository previousRepository, ObjectId commit = null) =>
            await ReloadRepositoryAsync(previousRepository.RepositoryDescription, commit).ConfigureAwait(false);

        private async Task<TRepository> ReloadRepositoryAsync(RepositoryDescription repositoryDescription, ObjectId commit = null)
        {
            var result = await _repositoryLoader.LoadFromAsync(this, repositoryDescription, commit).ConfigureAwait(false);
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
        public async Task<TRepository> CheckoutAsync(UniqueId id, string branchName, bool createNewBranch = false, string committish = null)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            var repository = this[id];
            using (_logger.BeginScope("Checking out repository '{Repository}' at '{BranchName}'.", repository.Id, branchName))
            {
                return await repository.ExecuteAsync(async r =>
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

                    var newRepository = await _repositoryLoader.LoadFromAsync(this, repository.RepositoryDescription, branch.Tip.Id).ConfigureAwait(false);
                    return AddOrReplace(newRepository);
                }).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<TRepository> FetchAsync(UniqueId id, FetchOptions options = null)
        {
            if (options == null)
            {
                options = new FetchOptions();
            }

            var repository = this[id];
            using (_logger.BeginScope("Fetching repository '{Repository}'.", repository.Id))
            {
                return await repository.ExecuteAsync(async r =>
                {
                    var concrete = r as Repository ??
                        throw new NotSupportedException($"Object of type {nameof(Repository)} expected.");
                    Commands.Fetch(concrete, r.Head.RemoteName, Array.Empty<string>(), options, null);

                    return await _repositoryLoader.LoadFromAsync(this, repository.RepositoryDescription, r.Head.TrackedBranch.Tip.Id).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task FetchAllAsync(UniqueId id, FetchOptions options = null)
        {
            if (options == null)
            {
                options = new FetchOptions();
            }

            var repository = this[id];
            using (_logger.BeginScope("Fetching all remotes for repository '{Repository}'.", repository.Id))
            {
                await repository.RepositoryProvider.ExecuteAsync(repository.RepositoryDescription, r =>
                {
                    var concrete = r as Repository ??
                        throw new NotSupportedException($"Object of type {nameof(Repository)} expected.");
                    foreach (var remote in r.Network.Remotes)
                    {
                        var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                        Commands.Fetch(concrete, remote.Name, refSpecs, options, null);
                    }
                }).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<IObjectRepositoryMerge> PullAsync(UniqueId id, FetchOptions options = null)
        {
            if (options == null)
            {
                options = new FetchOptions();
            }

            var repository = this[id];
            using (_logger.BeginScope("Pulling repository '{Repository}'.", repository.Id))
            {
                var (originTip, remoteBranch) = await repository.ExecuteAsync(r =>
                {
                    var concrete = r as Repository ??
                        throw new NotSupportedException($"Object of type {nameof(Repository)} expected.");
                    Commands.Fetch(concrete, r.Head.RemoteName, Array.Empty<string>(), options, null);

                    return (r.Head.TrackedBranch.Tip.Id, r.Head.TrackedBranch.FriendlyName);
                }).ConfigureAwait(false);
                return await _objectRepositoryMergeFactory(repository, originTip, remoteBranch).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<IObjectRepositoryMerge> MergeAsync(UniqueId id, string branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            var repository = this[id];
            using (_logger.BeginScope("Merging repository '{Repository}' with '{BranchName}'.", repository.Id, branchName))
            {
                var commitId = await repository.ExecuteAsync(r => r.Branches[branchName].Tip.Id).ConfigureAwait(false);
                return await _objectRepositoryMergeFactory(repository, commitId, branchName).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<IObjectRepositoryRebase> RebaseAsync(UniqueId id, string branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            var repository = this[id];
            using (_logger.BeginScope("Merging repository '{Repository}' with '{BranchName}'.", repository.Id, branchName))
            {
                var commitId = await repository.ExecuteAsync(r => r.Branches[branchName].Tip.Id).ConfigureAwait(false);
                return await _objectRepositoryRebaseFactory(repository, commitId, branchName).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<IObjectRepositoryCherryPick> CherryPickAsync(UniqueId id, ObjectId commitId)
        {
            if (commitId == null)
            {
                throw new ArgumentNullException(nameof(commitId));
            }

            var repository = this[id];
            using (_logger.BeginScope("Cherry picking commit '{CommitId}' in '{Repository}'.", commitId, repository.Id))
            {
                return await _objectRepositoryCherryPickFactory(repository, commitId).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public override ValidationResult Validate(ValidationRules rules = ValidationRules.All) =>
            new ValidationResult(Repositories.SelectMany(r => r.ValidateAsync(rules).Errors).ToList());
    }
}
