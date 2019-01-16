using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using GitObjectDb.Serialization;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public IObjectRepository Clone(IObjectRepositoryContainer container, string repository, RepositoryDescription repositoryDescription, ObjectId commitId = null)
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

            Repository.Init(repositoryDescription.Path);
            return _repositoryProvider.Execute(repositoryDescription, r =>
            {
                Clone(repository, r, commitId);
                return LoadFrom(container, repositoryDescription, r, commitId);
            });
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
        public TRepository Clone<TRepository>(IObjectRepositoryContainer<TRepository> container, string repository, RepositoryDescription repositoryDescription, ObjectId commitId = null)
            where TRepository : IObjectRepository
        {
            return (TRepository)Clone((IObjectRepositoryContainer)container, repository, repositoryDescription, commitId);
        }

        /// <inheritdoc />
        public IObjectRepository LoadFrom(IObjectRepositoryContainer container, RepositoryDescription repositoryDescription, ObjectId commitId = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            if (repositoryDescription == null)
            {
                throw new ArgumentNullException(nameof(repositoryDescription));
            }

            return _repositoryProvider.Execute(repositoryDescription, repository =>
                LoadFrom(container, repositoryDescription, repository, commitId));
        }

        private IObjectRepository LoadFrom(IObjectRepositoryContainer container, RepositoryDescription repositoryDescription, IRepository repository, ObjectId commitId = null)
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

            var instance = (IObjectRepository)LoadEntry(container, commitId, currentCommit[FileSystemStorage.DataFile], string.Empty);
            instance.SetRepositoryData(repositoryDescription, commitId);
            return instance;
        }

        /// <inheritdoc />
        public TRepository LoadFrom<TRepository>(IObjectRepositoryContainer<TRepository> container, RepositoryDescription repositoryDescription, ObjectId commitId = null)
            where TRepository : IObjectRepository
        {
            return (TRepository)LoadFrom((IObjectRepositoryContainer)container, repositoryDescription, commitId);
        }

        private IModelObject LoadEntry(IObjectRepositoryContainer container, ObjectId commitId, TreeEntry entry, string path)
        {
            var context = new ModelObjectSerializationContext(container, ResolveChildren);
            var serializer = _repositorySerializerFactory(context);
            var blob = (Blob)entry.Target;
            return serializer.Deserialize(blob.GetContentStream());

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
            LazyChildrenHelper.Create(childProperty, (parent, repository) =>
            {
                var childPath = string.IsNullOrEmpty(path) ? childProperty.FolderName : $"{path}/{childProperty.FolderName}";
                var commit = repository.Lookup<Commit>(commitId);
                var entry = commit[childPath];
                if (entry == null)
                {
                    return Enumerable.Empty<IModelObject>();
                }
                var subTree = (Tree)entry.Target;
                return subTree.Any() ?
                    LoadEntryChildren(container, commitId, childPath, subTree) :
                    Enumerable.Empty<IModelObject>();
            });

        private IEnumerable<IModelObject> LoadEntryChildren(IObjectRepositoryContainer container, ObjectId commitId, string childPath, Tree subTree) =>
            from c in subTree
            where c.TargetType == TreeEntryTargetType.Tree
            let childTree = (Tree)c.Target
            let data = childTree[FileSystemStorage.DataFile]
            where data != null
            select LoadEntry(container, commitId, data, $"{childPath}/{c.Name}");
    }
}
