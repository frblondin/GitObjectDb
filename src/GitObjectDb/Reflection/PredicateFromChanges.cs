using GitObjectDb.JsonConverters;
using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Predicate that translates the changes described in chunk change descriptions.
    /// </summary>
    /// <seealso cref="GitObjectDb.Reflection.IPredicateReflector" />
    internal class PredicateFromChanges : IPredicateReflector
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ModelObjectContractResolverFactory _contractResolverFactory;
        private readonly IObjectRepositoryContainer _container;
        private readonly ILookup<UniqueId, ObjectRepositoryChunkChange> _modifiedChunks;
        private readonly ILookup<UniqueId, ObjectRepositoryAdd> _addedObjects;
        private readonly ISet<UniqueId> _deletedObjects;
        private readonly IEnumerable<string> _impactedPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="PredicateFromChanges"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="container">The container.</param>
        /// <param name="modifiedChunks">The modified chunks.</param>
        /// <param name="addedObjects">The added objects.</param>
        /// <param name="deletedObjects">The deleted objects.</param>
        public PredicateFromChanges(IServiceProvider serviceProvider, IObjectRepositoryContainer container, IList<ObjectRepositoryChunkChange> modifiedChunks, IList<ObjectRepositoryAdd> addedObjects, IList<ObjectRepositoryDelete> deletedObjects)
        {
            if (modifiedChunks == null)
            {
                throw new ArgumentNullException(nameof(modifiedChunks));
            }
            if (addedObjects == null)
            {
                throw new ArgumentNullException(nameof(addedObjects));
            }
            if (deletedObjects == null)
            {
                throw new ArgumentNullException(nameof(deletedObjects));
            }

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _contractResolverFactory = _serviceProvider.GetRequiredService<ModelObjectContractResolverFactory>();
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _modifiedChunks = modifiedChunks.ToLookup(c => c.Id);
            _addedObjects = addedObjects.ToLookup(o => o.ParentId);
            _deletedObjects = new HashSet<UniqueId>(deletedObjects.Select(o => o.Id));
            _impactedPaths = modifiedChunks.Select(c => c.Path)
                .Concat(addedObjects.Select(o => o.Path))
                .Concat(deletedObjects.Select(o => o.Path))
                .Distinct()
                .ToList();
        }

        /// <inheritdoc/>
        public (IEnumerable<IModelObject> Additions, IEnumerable<IModelObject> Deletions) GetChildChanges(IModelObject instance, ChildPropertyInfo childProperty)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (childProperty == null)
            {
                throw new ArgumentNullException(nameof(childProperty));
            }

            var context = new ModelObjectSerializationContext(_container);
            var serializer = _contractResolverFactory(context).Serializer;
            return (GetAdditions(instance, serializer), GetDeletions(instance, childProperty));
        }

        private IEnumerable<IModelObject> GetAdditions(IModelObject instance, JsonSerializer serializer) =>
            _addedObjects[instance.Id].Select(o => Parse(o.Node, serializer));

        private static IModelObject Parse(JObject jObject, JsonSerializer serializer)
        {
            var type = Type.GetType(jObject.Value<string>("$type"));
            return (IModelObject)jObject.ToObject(type, serializer);
        }

        IEnumerable<IModelObject> GetDeletions(IModelObject instance, ChildPropertyInfo childProperty)
        {
            if (_deletedObjects.Any())
            {
                var children = childProperty.Accessor(instance);
                return children.Where(c => IsDeleted(c));
            }
            else
            {
                return Enumerable.Empty<IModelObject>();
            }
        }

        private bool IsDeleted(IModelObject instance)
        {
            if (_deletedObjects.Contains(instance.Id))
            {
                return true;
            }
            if (instance.Parent != null)
            {
                return IsDeleted(instance.Parent);
            }
            return false;
        }

        /// <inheritdoc/>
        public object ProcessArgument(IModelObject instance, string name, Type argumentType, object fallback = null)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (argumentType == null)
            {
                throw new ArgumentNullException(nameof(argumentType));
            }

            var change = _modifiedChunks[instance.Id]
                .FirstOrDefault(c => c.Property.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (change != null)
            {
                var propertyType = change.Property.Property.PropertyType;
                return change.MergeValue.ToObject(propertyType);
            }

            return fallback is ICloneable cloneable ? cloneable.Clone() : fallback;
        }

        /// <inheritdoc/>
        public bool MustForceVisit(IModelObject node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var folderPath = node.GetFolderPath();
            return _impactedPaths.Any(p => p.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase));
        }
    }
}
