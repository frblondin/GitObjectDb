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
    internal class InstanceLoader : IInstanceLoader
    {
        /// <summary>
        /// The data file name used to store information in Git.
        /// </summary>
        internal const string DataFile = "data.json";

        readonly IContractResolver _contractResolver = new DefaultContractResolver();
        readonly IServiceProvider _serviceProvider;
        readonly IModelDataAccessorProvider _dataAccessorProvider;
        readonly IRepositoryProvider _repositoryProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceLoader"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public InstanceLoader(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _dataAccessorProvider = _serviceProvider.GetRequiredService<IModelDataAccessorProvider>();
            _repositoryProvider = _serviceProvider.GetRequiredService<IRepositoryProvider>();
        }

        /// <inheritdoc />
        public TInstance LoadFrom<TInstance>(RepositoryDescription repositoryDescription, Func<IRepository, Tree> tree)
            where TInstance : AbstractInstance
        {
            if (repositoryDescription == null)
            {
                throw new ArgumentNullException(nameof(repositoryDescription));
            }
            if (tree == null)
            {
                throw new ArgumentNullException(nameof(tree));
            }

            return _repositoryProvider.Execute(repositoryDescription, repository =>
            {
                var currentTree = tree(repository);
                var instance = (TInstance)LoadEntry(tree, currentTree[DataFile], string.Empty);
                instance.SetRepositoryData(repositoryDescription, tree);
                return instance;
            });
        }

        IMetadataObject LoadEntry(Func<IRepository, Tree> tree, TreeEntry entry, string path)
        {
            var blob = entry.Target.Peel<Blob>();
            ILazyChildren ResolveChildren(Type type, string propertyName)
            {
                var dataAccessor = _dataAccessorProvider.Get(type);
                var childProperty = dataAccessor.ChildProperties.FirstOrDefault(p => p.Matches(propertyName)) ??
                    throw new NotSupportedException($"Unable to find property details for '{propertyName}'.");
                return LoadEntryChildren(tree, path, childProperty);
            }
            var serializer = GetJsonSerializer(ResolveChildren);
            var jobject = blob.GetContentStream().ToJson<JObject>(serializer);
            var objectType = Type.GetType(jobject.Value<string>("$type"));
            return (IMetadataObject)jobject.ToObject(objectType, serializer);
        }

        JsonSerializer GetJsonSerializer(MetadataObjectJsonConverter.ChildrenResolver childrenResolver)
        {
            var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Objects };
            serializer.Converters.Add(new MetadataObjectJsonConverter(_serviceProvider, childrenResolver));

            // Optimization: prevent reflection for each new object!
            serializer.ContractResolver = _contractResolver;

            return serializer;
        }

        ILazyChildren LoadEntryChildren(Func<IRepository, Tree> tree, string path, ChildPropertyInfo childProperty) =>
            LazyChildrenHelper.Create(childProperty, (parent, repository) =>
            {
                var childPath = string.IsNullOrEmpty(path) ? childProperty.Property.Name : $"{path}/{childProperty.Property.Name}";
                var subTree = tree(repository)[childPath]?.Target.Peel<Tree>();
                return (subTree?.Any() ?? false) ?

                    from c in subTree
                    where c.TargetType == TreeEntryTargetType.Tree
                    let childTree = c.Target.Peel<Tree>()
                    let data = childTree[DataFile]
                    where data != null
                    select LoadEntry(tree, data, $"{childPath}/{c.Name}") :

                    Enumerable.Empty<IMetadataObject>();
            });
    }
}
