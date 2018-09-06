using GitObjectDb.Git;
using GitObjectDb.JsonConverters;
using GitObjectDb.Reflection;
using LibGit2Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models
{
    /// <inheritdoc />
    internal class ObjectRepositoryLoader : IObjectRepositoryLoader
    {
        readonly IContractResolver _contractResolver = new DefaultContractResolver();
        readonly IServiceProvider _serviceProvider;
        readonly IModelDataAccessorProvider _dataAccessorProvider;
        readonly IRepositoryProvider _repositoryProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryLoader"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public ObjectRepositoryLoader(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _dataAccessorProvider = _serviceProvider.GetRequiredService<IModelDataAccessorProvider>();
            _repositoryProvider = _serviceProvider.GetRequiredService<IRepositoryProvider>();
        }

        /// <inheritdoc />
        public AbstractObjectRepository Clone(IObjectRepositoryContainer container, string repository, RepositoryDescription repositoryDescription, ObjectId commitId = null)
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
            where TRepository : AbstractObjectRepository
        {
            return (TRepository)Clone((IObjectRepositoryContainer)container, repository, repositoryDescription, commitId);
        }

        /// <inheritdoc />
        public AbstractObjectRepository LoadFrom(IObjectRepositoryContainer container, RepositoryDescription repositoryDescription, ObjectId commitId = null)
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

                var instance = (AbstractObjectRepository)LoadEntry(container, commitId, currentCommit[FileSystemStorage.DataFile], string.Empty);
                instance.SetRepositoryData(repositoryDescription, commitId);
                return instance;
            });
        }

        /// <inheritdoc />
        public TRepository LoadFrom<TRepository>(IObjectRepositoryContainer<TRepository> container, RepositoryDescription repositoryDescription, ObjectId commitId = null)
            where TRepository : AbstractObjectRepository
        {
            return (TRepository)LoadFrom((IObjectRepositoryContainer)container, repositoryDescription, commitId);
        }

        IMetadataObject LoadEntry(IObjectRepositoryContainer container, ObjectId commitId, TreeEntry entry, string path)
        {
            ILazyChildren ResolveChildren(Type type, string propertyName)
            {
                var dataAccessor = _dataAccessorProvider.Get(type);
                var childProperty = dataAccessor.ChildProperties.FirstOrDefault(
                    p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                if (childProperty == null)
                {
                    throw new NotSupportedException($"Unable to find property details for '{propertyName}'.");
                }
                return LoadEntryChildren(container, commitId, path, childProperty);
            }
            var serializer = GetJsonSerializer(container, ResolveChildren);
            var blob = (Blob)entry.Target;
            var jobject = blob.GetContentStream().ToJson<JObject>(serializer);
            var objectType = Type.GetType(jobject.Value<string>("$type"));
            return (IMetadataObject)jobject.ToObject(objectType, serializer);
        }

        /// <inheritdoc />
        public JsonSerializer GetJsonSerializer(IObjectRepositoryContainer container, ChildrenResolver childrenResolver = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            if (childrenResolver == null)
            {
                childrenResolver = ReturnEmptyChildren;
            }

            var serializer = new JsonSerializer
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                TypeNameHandling = TypeNameHandling.Objects,
                Formatting = Formatting.Indented
            };
            serializer.Converters.Add(new MetadataObjectJsonConverter(_serviceProvider, childrenResolver, container));
            serializer.Converters.Add(new VersionConverter());

            // Optimization: prevent reflection for each new object!
            serializer.ContractResolver = _contractResolver;

            return serializer;
        }

        ILazyChildren ReturnEmptyChildren(Type parentType, string propertyName)
        {
            var dataAccessor = _dataAccessorProvider.Get(parentType);
            var childProperty = dataAccessor.ChildProperties.FirstOrDefault(
                p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            return LazyChildrenHelper.Create(childProperty, (o, r) => Enumerable.Empty<IMetadataObject>());
        }

        ILazyChildren LoadEntryChildren(IObjectRepositoryContainer container, ObjectId commitId, string path, ChildPropertyInfo childProperty) =>
            LazyChildrenHelper.Create(childProperty, (parent, repository) =>
            {
                var childPath = string.IsNullOrEmpty(path) ? childProperty.FolderName : $"{path}/{childProperty.FolderName}";
                var commit = repository.Lookup<Commit>(commitId);
                var subTree = (Tree)commit[childPath]?.Target;
                return (subTree?.Any() ?? false) ?
                    LoadEntryChildren(container, commitId, childPath, subTree) :
                    Enumerable.Empty<IMetadataObject>();
            });

        IEnumerable<IMetadataObject> LoadEntryChildren(IObjectRepositoryContainer container, ObjectId commitId, string childPath, Tree subTree) =>
            from c in subTree
            where c.TargetType == TreeEntryTargetType.Tree
            let childTree = (Tree)c.Target
            let data = childTree[FileSystemStorage.DataFile]
            where data != null
            select LoadEntry(container, commitId, data, $"{childPath}/{c.Name}");
    }
}
