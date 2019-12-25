using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using GitObjectDb.Serialization;
using GitObjectDb.Threading;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.Services
{
    /// <inheritdoc />
    internal class ObjectRepositoryLoader : IObjectRepositoryLoader
    {
        private readonly IModelDataAccessorProvider _dataAccessorProvider;
        private readonly IRepositoryProvider _repositoryProvider;
        private readonly ObjectRepositorySerializerFactory _repositorySerializerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryLoader"/> class.
        /// </summary>
        /// <param name="dataAccessorProvider">The data accessor provider.</param>
        /// <param name="repositoryProvider">The repository provider.</param>
        /// <param name="repositorySerializerFactory">The <see cref="IObjectRepositorySerializer"/> factory.</param>
        public ObjectRepositoryLoader(IModelDataAccessorProvider dataAccessorProvider, IRepositoryProvider repositoryProvider,
            ObjectRepositorySerializerFactory repositorySerializerFactory)
        {
            _dataAccessorProvider = dataAccessorProvider ?? throw new ArgumentNullException(nameof(dataAccessorProvider));
            _repositoryProvider = repositoryProvider ?? throw new ArgumentNullException(nameof(repositoryProvider));
            _repositorySerializerFactory = repositorySerializerFactory ?? throw new ArgumentNullException(nameof(repositorySerializerFactory));
        }

        /// <inheritdoc />
        public async Task<IObjectRepository> CloneAsync(IObjectRepositoryContainer container, string repository, RepositoryDescription repositoryDescription, ObjectId commitId = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (repositoryDescription == null)
            {
                throw new ArgumentNullException(nameof(repositoryDescription));
            }

            await RepositoryTaskScheduler.ExecuteAsync(() => Repository.Init(repositoryDescription.Path)).ConfigureAwait(false);
            return await _repositoryProvider.ExecuteAsync(repositoryDescription, async r =>
            {
                Clone(repository, r, commitId);
                return await LoadFromAsync(container, repositoryDescription, r, commitId).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private static void Clone(string repository, IRepository r, ObjectId commitId = null)
        {
            const string OriginRemote = "origin";
            const string MasterBranch = "master";

            var concrete = r as Repository ??
                throw new NotSupportedException($"Object of type {nameof(Repository)} expected.");
            r.Network.Remotes.Add(OriginRemote, repository);
            Commands.Fetch(concrete, OriginRemote, new[] { MasterBranch }, new FetchOptions { TagFetchMode = TagFetchMode.All }, null);
            var masterBranch = r.Branches.Add(MasterBranch, commitId?.Sha ?? r.Branches["origin/master"].Tip.Sha);
            r.Branches.Update(masterBranch,
                b => b.Remote = OriginRemote,
                b => b.UpstreamBranch = MasterBranch,
                b => b.TrackedBranch = $"refs/remotes/{OriginRemote}/{MasterBranch}");
        }

        /// <inheritdoc />
        public async Task<TRepository> CloneAsync<TRepository>(IObjectRepositoryContainer<TRepository> container, string repository, RepositoryDescription repositoryDescription, ObjectId commitId = null)
            where TRepository : class, IObjectRepository
        {
            return (TRepository)await CloneAsync((IObjectRepositoryContainer)container, repository, repositoryDescription, commitId).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IObjectRepository> LoadFromAsync(IObjectRepositoryContainer container, RepositoryDescription repositoryDescription, ObjectId commitId = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            if (repositoryDescription == null)
            {
                throw new ArgumentNullException(nameof(repositoryDescription));
            }

            return await _repositoryProvider.ExecuteAsync(repositoryDescription, repository =>
                LoadFromAsync(container, repositoryDescription, repository, commitId)).ConfigureAwait(false);
        }

        private async Task<IObjectRepository> LoadFromAsync(IObjectRepositoryContainer container, RepositoryDescription repositoryDescription, IRepository repository, ObjectId commitId = null)
        {
            var currentCommit = GetCurrentCommit(repository, ref commitId);
            var entry = currentCommit[FileSystemStorage.DataFile];

            var instance = (IObjectRepository)await LoadEntryAsync(container, commitId, entry, string.Empty, RelativeFileDataResolver).ConfigureAwait(false);
            instance.SetRepositoryData(repositoryDescription, commitId);
            return instance;

            string RelativeFileDataResolver(string relativePath) => (currentCommit[relativePath]?.Target as Blob)?.GetContentText() ?? string.Empty;
        }

        private static Commit GetCurrentCommit(IRepository repository, ref ObjectId commitId)
        {
            Commit currentCommit;
            if (commitId == null)
            {
                currentCommit = repository.Head.Tip;
                commitId = currentCommit.Id;
            }
            else
            {
                currentCommit = repository.Lookup<Commit>(commitId);
            }

            return currentCommit;
        }

        /// <inheritdoc />
        public async Task<TRepository> LoadFromAsync<TRepository>(IObjectRepositoryContainer<TRepository> container, RepositoryDescription repositoryDescription, ObjectId commitId = null)
            where TRepository : class, IObjectRepository
        {
            return (TRepository)await LoadFromAsync((IObjectRepositoryContainer)container, repositoryDescription, commitId).ConfigureAwait(false);
        }

        private async Task<IModelObject> LoadEntryAsync(IObjectRepositoryContainer container, ObjectId commitId, TreeEntry entry, string path, Func<string, string> relativeFileDataResolver)
        {
            var context = new ModelObjectSerializationContext(container, ResolveChildren);
            var serializer = _repositorySerializerFactory(context);
            var content = await RepositoryTaskScheduler.ExecuteAsync(() => ((Blob)entry.Target).GetContentText()).ConfigureAwait(false);
            return serializer.Deserialize(content, relativeFileDataResolver);

            ILazyChildren ResolveChildren(Type type, string propertyName)
            {
                var dataAccessor = _dataAccessorProvider.Get(type);
                var childProperty = dataAccessor.ChildProperties.TryGetWithValue(p => p.Name, propertyName);
                if (childProperty == null)
                {
                    throw new GitObjectDbException($"Unable to find property details for '{propertyName}'.");
                }
                return LoadEntryChildren(container, commitId, path, childProperty);
            }
        }

        private ILazyChildren LoadEntryChildren(IObjectRepositoryContainer container, ObjectId commitId, string path, ChildPropertyInfo childProperty) =>
            LazyChildrenHelper.Create(childProperty, async (parent, repository) =>
            {
                var childPath = string.IsNullOrEmpty(path) ? childProperty.FolderName : $"{path}/{childProperty.FolderName}";
                var commit = repository.Lookup<Commit>(commitId);
                var entry = commit[childPath];
                if (entry == null)
                {
                    return ImmutableList.Create<IModelObject>();
                }
                var subTree = (Tree)entry.Target;
                return subTree.Any() ?
                    await LoadEntryChildrenAsync(container, commitId, childPath, subTree).ConfigureAwait(false) :
                    ImmutableList.Create<IModelObject>();
            });

        private async Task<IImmutableList<IModelObject>> LoadEntryChildrenAsync(IObjectRepositoryContainer container, ObjectId commitId, string childPath, Tree subTree)
        {
            var result = ImmutableList.CreateBuilder<IModelObject>();
            foreach (var c in subTree)
            {
                if (c.TargetType == TreeEntryTargetType.Tree)
                {
                    var childTree = (Tree)c.Target;
                    var data = childTree[FileSystemStorage.DataFile];
                    if (data == null)
                    {
                        continue;
                    }
                    var entry = await LoadEntryAsync(container, commitId, data, $"{childPath}/{c.Name}",
                        relativePath => (subTree[relativePath]?.Target as Blob)?.GetContentText() ?? string.Empty).ConfigureAwait(false);
                    result.Add(entry);
                }
            }
            return result.ToImmutable();
        }
    }
}
