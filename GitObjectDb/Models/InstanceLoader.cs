using GitObjectDb.Utils;
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
    public static class InstanceLoader
    {
        internal const string DataFile = "data.json";

        static readonly IContractResolver _contractResolver = new DefaultContractResolver();

        public static TInstance LoadFrom<TInstance>(IServiceProvider serviceProvider, Func<Repository> repositoryFactory, Func<Repository, Tree> tree) where TInstance : AbstractInstance
        {
            using (var repository = repositoryFactory())
            {
                var dataAccessorProvider = serviceProvider.GetService<IModelDataAccessorProvider>();
                var currentTree = tree(repository);
                var instance = (TInstance)LoadEntry(serviceProvider, dataAccessorProvider, tree, currentTree[DataFile], "");
                instance.SetRepositoryData(repositoryFactory, tree);
                return instance;
            }
        }

        static IMetadataObject LoadEntry(IServiceProvider serviceProvider, IModelDataAccessorProvider dataAccessorProvider, Func<Repository, Tree> tree, TreeEntry entry, string path)
        {
            var blob = entry.Target.Peel<Blob>();
            ILazyChildren ResolveChildren(Type type, string propertyName)
            {
                var dataAccessor = dataAccessorProvider.Get(type);
                var childProperty = dataAccessor.ChildProperties.FirstOrDefault(p => p.Matches(propertyName)) ??
                    throw new NotSupportedException($"Unable to find property details for '{propertyName}'.");
                return LoadEntryChildren(serviceProvider, dataAccessorProvider, tree, path, propertyName, childProperty);
            }
            var serializer = GetJsonSerializer(serviceProvider, dataAccessorProvider, tree, path, ResolveChildren);
            var jobject = blob.GetContentStream().ToJson<JObject>(serializer);
            var objectType = Type.GetType(jobject.Value<string>("$type"));
            return (IMetadataObject)jobject.ToObject(objectType, serializer);
        }

        static JsonSerializer GetJsonSerializer(IServiceProvider serviceProvider, IModelDataAccessorProvider dataAccessorProvider, Func<Repository, Tree> tree, string path, ChildrenResolver childrenResolver)
        {
            var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Objects };
            serializer.Converters.Add(new MetadataObjectJsonConverter(serviceProvider, childrenResolver));

            // Optimization: prevent reflection for each new object!
            serializer.ContractResolver = _contractResolver;

            return serializer;
        }

        static ILazyChildren LoadEntryChildren(IServiceProvider serviceProvider, IModelDataAccessorProvider dataAccessorProvider, Func<Repository, Tree> tree, string path, string propertyName, ChildPropertyInfo childProperty) =>
            LazyChildren.Create(childProperty, (parent, repository) =>
            {
                var childPath = string.IsNullOrEmpty(path) ? childProperty.Property.Name : $"{path}/{childProperty.Property.Name}";
                var subTree = tree(repository)[childPath]?.Target.Peel<Tree>();
                return (subTree?.Any() ?? false) ?

                    from c in subTree
                    where c.TargetType == TreeEntryTargetType.Tree
                    let childTree = c.Target.Peel<Tree>()
                    let data = childTree[DataFile]
                    where data != null
                    select LoadEntry(serviceProvider, dataAccessorProvider, tree, data, $"{childPath}/{c.Name}") :

                    Enumerable.Empty<IMetadataObject>();
            });
    }
}
