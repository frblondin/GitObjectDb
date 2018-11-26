using GitObjectDb.Git;
using GitObjectDb.JsonConverters;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Services
{
    /// <inheritdoc />
    internal class ObjectRepositoryLoader : IObjectRepositoryLoader
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IModelDataAccessorProvider _dataAccessorProvider;
        private readonly IRepositoryProvider _repositoryProvider;
        private readonly ModelObjectContractResolverFactory _contractResolverFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryLoader"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public ObjectRepositoryLoader(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _dataAccessorProvider = _serviceProvider.GetRequiredService<IModelDataAccessorProvider>();
            _repositoryProvider = _serviceProvider.GetRequiredService<IRepositoryProvider>();
            _contractResolverFactory = _serviceProvider.GetRequiredService<ModelObjectContractResolverFactory>();
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

            Repository.Clone(repository, repositoryDescription.Path, new CloneOptions { Checkout = false });
            return LoadFrom(container, repositoryDescription, commitId);
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
            });
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
            var serializer = _contractResolverFactory(context).Serializer;
            var blob = (Blob)entry.Target;
            return blob.GetContentStream().ToJson<IModelObject>(serializer);

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
