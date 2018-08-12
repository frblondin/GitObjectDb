using GitObjectDb.Git;
using GitObjectDb.JsonConverters;
using GitObjectDb.Reflection;
using LibGit2Sharp;
using Newtonsoft.Json;
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
        public AbstractObjectRepository Clone(string repository, RepositoryDescription repositoryDescription, ObjectId commitId = null)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (repositoryDescription == null)
            {
                throw new ArgumentNullException(nameof(repositoryDescription));
            }

            Repository.Clone(repository, repositoryDescription.Path, new CloneOptions { Checkout = false });
            return LoadFrom(repositoryDescription, commitId);
        }

        /// <inheritdoc />
        public TRepository Clone<TRepository>(string repository, RepositoryDescription repositoryDescription, ObjectId commitId = null)
            where TRepository : AbstractObjectRepository
        {
            return (TRepository)Clone(repository, repositoryDescription, commitId);
        }

        /// <inheritdoc />
        public AbstractObjectRepository LoadFrom(RepositoryDescription repositoryDescription, ObjectId commitId = null)
        {
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

                var instance = (AbstractObjectRepository)LoadEntry(commitId, currentCommit[FileSystemStorage.DataFile], string.Empty);
                instance.SetRepositoryData(repositoryDescription, commitId);
                return instance;
            });
        }

        /// <inheritdoc />
        public TInstance LoadFrom<TInstance>(RepositoryDescription repositoryDescription, ObjectId commitId = null)
            where TInstance : AbstractObjectRepository
        {
            return (TInstance)LoadFrom(repositoryDescription, commitId);
        }

        IMetadataObject LoadEntry(ObjectId commitId, TreeEntry entry, string path)
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
                return LoadEntryChildren(commitId, path, childProperty);
            }
            var serializer = GetJsonSerializer(ResolveChildren);
            var blob = (Blob)entry.Target;
            var jobject = blob.GetContentStream().ToJson<JObject>(serializer);
            var objectType = Type.GetType(jobject.Value<string>("$type"));
            return (IMetadataObject)jobject.ToObject(objectType, serializer);
        }

        /// <inheritdoc />
        public JsonSerializer GetJsonSerializer(ChildrenResolver childrenResolver = null)
        {
            if (childrenResolver == null)
            {
                childrenResolver = ReturnEmptyChildren;
            }

            var serializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.Objects,
                Formatting = Formatting.Indented
            };
            serializer.Converters.Add(new MetadataObjectJsonConverter(_serviceProvider, childrenResolver));

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

        ILazyChildren LoadEntryChildren(ObjectId commitId, string path, ChildPropertyInfo childProperty) =>
            LazyChildrenHelper.Create(childProperty, (parent, repository) =>
            {
                var childPath = string.IsNullOrEmpty(path) ? childProperty.Name : $"{path}/{childProperty.Name}";
                var commit = repository.Lookup<Commit>(commitId);
                var subTree = (Tree)commit[childPath]?.Target;
                return (subTree?.Any() ?? false) ?
                    LoadEntryChildren(commitId, childPath, subTree) :
                    Enumerable.Empty<IMetadataObject>();
            });

        IEnumerable<IMetadataObject> LoadEntryChildren(ObjectId commitId, string childPath, Tree subTree) =>
            from c in subTree
            where c.TargetType == TreeEntryTargetType.Tree
            let childTree = (Tree)c.Target
            let data = childTree[FileSystemStorage.DataFile]
            where data != null
            select LoadEntry(commitId, data, $"{childPath}/{c.Name}");
    }
}
