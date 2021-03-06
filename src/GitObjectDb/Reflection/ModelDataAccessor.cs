using GitObjectDb.Attributes;
using GitObjectDb.Models;
using GitObjectDb.Serialization;
using GitObjectDb.Transformations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Reflection
{
    /// <inheritdoc />
    internal partial class ModelDataAccessor : IModelDataAccessor
    {
        private readonly Lazy<IImmutableList<ChildPropertyInfo>> _childProperties;
        private readonly Lazy<IImmutableList<ModifiablePropertyInfo>> _modifiableProperties;
        private readonly Lazy<ConstructorParameterBinding> _constructorBinding;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelDataAccessor"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="constructorParameterBindingFactory">The <see cref="ConstructorParameterBinding"/> factory.</param>
        /// <param name="repositorySerializerFactory">The <see cref="IObjectRepositorySerializer"/> factory.</param>
        /// <param name="constructorSelector">The constructor selector.</param>
        [ActivatorUtilitiesConstructor]
        public ModelDataAccessor(Type type,
            ConstructorParameterBinding.Factory constructorParameterBindingFactory, ObjectRepositorySerializerFactory repositorySerializerFactory,
            IConstructorSelector constructorSelector)
        {
            if (constructorParameterBindingFactory == null)
            {
                throw new ArgumentNullException(nameof(constructorParameterBindingFactory));
            }
            if (repositorySerializerFactory == null)
            {
                throw new ArgumentNullException(nameof(repositorySerializerFactory));
            }
            if (constructorSelector == null)
            {
                throw new ArgumentNullException(nameof(constructorSelector));
            }

            Type = type ?? throw new ArgumentNullException(nameof(type));

            _childProperties = new Lazy<IImmutableList<ChildPropertyInfo>>(GetChildProperties);
            _modifiableProperties = new Lazy<IImmutableList<ModifiablePropertyInfo>>(GetModifiableProperties);
            _constructorBinding = new Lazy<ConstructorParameterBinding>(() =>
            {
                var constructors = from c in Type.GetTypeInfo().GetConstructors()
                                   select constructorParameterBindingFactory(c);
                return constructorSelector.SelectConstructorBinding(Type, constructors.ToArray());
            });

            repositorySerializerFactory().ValidateSerializable(Type);
        }

        /// <inheritdoc />
        public Type Type { get; }

        /// <inheritdoc />
        public IImmutableList<ChildPropertyInfo> ChildProperties => _childProperties.Value;

        /// <inheritdoc />
        public IImmutableList<ModifiablePropertyInfo> ModifiableProperties => _modifiableProperties.Value;

        /// <inheritdoc />
        public ConstructorParameterBinding ConstructorParameterBinding => _constructorBinding.Value;

        private IImmutableList<ChildPropertyInfo> GetChildProperties() =>
            (from p in Type.GetTypeInfo().GetProperties()
             let lazyChildrenType = LazyChildrenHelper.TryGetLazyChildrenInterface(p.PropertyType)
             where lazyChildrenType != null
             select new ChildPropertyInfo(p, lazyChildrenType.GetGenericArguments()[0]))
            .ToImmutableList();

        private IImmutableList<ModifiablePropertyInfo> GetModifiableProperties() =>
            (from p in Type.GetTypeInfo().GetProperties()
             where Attribute.IsDefined(p, typeof(ModifiableAttribute))
             select new ModifiablePropertyInfo(p))
            .ToImmutableList();

        /// <inheritdoc />
        public IObjectRepository With(IObjectRepository repository, IEnumerable<ITransformation> transformations)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (transformations == null)
            {
                throw new ArgumentNullException(nameof(transformations));
            }

            var changes = transformations
                .OfType<PropertyTransformation>()
                .ToLookup(t => (t.InstanceId, t.PropertyName), TransformationLookupComparer.Instance);
            var additions = transformations
                .OfType<ChildAddTransformation>()
                .ToLookup(t => (t.InstanceId, t.PropertyName), t => t.Child, TransformationLookupComparer.Instance);
            var deletions = new HashSet<UniqueId>(transformations
                .OfType<ChildDeleteTransformation>().Select(d => d.ChildId));
            var impactedPaths = transformations.Select(c => c.Path).Distinct().ToList();
            return DeepClone(repository, ProcessArgument, GetChildChanges, MustForceVisit);

            object ProcessArgument(IModelObject instance, string name, Type argumentType, object fallback = null)
            {
                var match = changes[(instance.Id, name)].FirstOrDefault();
                if (match != null)
                {
                    return match.Value;
                }
                return fallback is ICloneable cloneable ? cloneable.Clone() : fallback;
            }
            (IEnumerable<IModelObject> Additions, IEnumerable<IModelObject> Deletions) GetChildChanges(IModelObject instance, ChildPropertyInfo childProperty)
            {
                var childrenToBeAdded = additions[(instance.Id, childProperty.FolderName)];
                var childrenToBeRemoved = childProperty.Accessor(instance).Where(c => IsDeleted(c));
                return (childrenToBeAdded, childrenToBeRemoved);
            }
            bool IsDeleted(IModelObject instance)
            {
                return instance.Parents().Any(p => deletions.Contains(p.Id));
            }
            bool MustForceVisit(IModelObject node)
            {
                var folderPath = node.GetFolderPath();
                return impactedPaths.Any(p => p.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <inheritdoc />
        public IObjectRepository DeepClone(IObjectRepository instance, ProcessArgument processArgument, ChildChangesGetter childChangesGetter, Func<IModelObject, bool> mustForceVisit)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return (IObjectRepository)DeepClone((IModelObject)instance, processArgument, childChangesGetter, mustForceVisit);
        }

        private IModelObject DeepClone(IModelObject node, ProcessArgument processArgument, ChildChangesGetter childChangesGetter, Func<IModelObject, bool> mustForceVisit)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            if (processArgument == null)
            {
                throw new ArgumentNullException(nameof(processArgument));
            }
            if (childChangesGetter == null)
            {
                throw new ArgumentNullException(nameof(childChangesGetter));
            }
            if (mustForceVisit == null)
            {
                throw new ArgumentNullException(nameof(mustForceVisit));
            }

            ILazyChildren ProcessChildren(ChildPropertyInfo childProperty, ILazyChildren children, IModelObject @new, IModelDataAccessor childDataAccessor)
            {
                var childChanges = childChangesGetter.Invoke(node, childProperty);
                return children.Clone(
                    forceVisit: mustForceVisit(children.Parent),
                    update: n => DeepClone(n, processArgument, childChangesGetter, mustForceVisit),
                    added: childChanges.Additions,
                    deleted: childChanges.Deletions);
            }

            return node.DataAccessor.ConstructorParameterBinding.Cloner(node, processArgument, ProcessChildren);
        }
    }
}
