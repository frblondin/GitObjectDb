using GitObjectDb.Attributes;
using GitObjectDb.Models;
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
    internal class ModelDataAccessor : IModelDataAccessor
    {
        readonly Lazy<IImmutableList<ChildPropertyInfo>> _childProperties;
        readonly Lazy<IImmutableList<ModifiablePropertyInfo>> _modifiableProperties;
        readonly Lazy<ConstructorParameterBinding> _constructorBinding;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelDataAccessor"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="type">The type.</param>
        /// <exception cref="ArgumentNullException">
        /// serviceProvider
        /// or
        /// IModelDataAccessorProvider
        /// or
        /// type
        /// </exception>
        public ModelDataAccessor(IServiceProvider serviceProvider, Type type)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            Type = type ?? throw new ArgumentNullException(nameof(type));

            _childProperties = new Lazy<IImmutableList<ChildPropertyInfo>>(GetChildProperties);
            _modifiableProperties = new Lazy<IImmutableList<ModifiablePropertyInfo>>(GetModifiableProperties);
            _constructorBinding = new Lazy<ConstructorParameterBinding>(() =>
            {
                var constructors = from c in Type.GetConstructors()
                                   select new ConstructorParameterBinding(serviceProvider, c);
                return serviceProvider.GetRequiredService<IConstructorSelector>().SelectConstructorBinding(Type, constructors.ToArray());
            });
        }

        /// <inheritdoc />
        public Type Type { get; }

        /// <inheritdoc />
        public IImmutableList<ChildPropertyInfo> ChildProperties => _childProperties.Value;

        /// <inheritdoc />
        public IImmutableList<ModifiablePropertyInfo> ModifiableProperties => _modifiableProperties.Value;

        /// <inheritdoc />
        public ConstructorParameterBinding ConstructorParameterBinding => _constructorBinding.Value;

        IImmutableList<ChildPropertyInfo> GetChildProperties() =>
            (from p in Type.GetProperties()
             let lazyChildrenType = LazyChildrenHelper.TryGetLazyChildrenInterface(p.PropertyType)
             where lazyChildrenType != null
             select new ChildPropertyInfo(p, lazyChildrenType.GetGenericArguments()[0]))
            .ToImmutableList();

        IImmutableList<ModifiablePropertyInfo> GetModifiableProperties() =>
            (from p in Type.GetProperties()
             where Attribute.IsDefined(p, typeof(ModifiableAttribute))
             select new ModifiablePropertyInfo(p))
            .ToImmutableList();

        /// <inheritdoc />
        public IMetadataObject With(IMetadataObject source, IPredicateReflector predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var newInstance = DeepClone(
                source.Repository,
                predicate.ProcessArgument,
                predicate.GetChildChanges,
                n => n.IsParentOf(source));
            return newInstance.TryGetFromGitPath(source.GetDataPath());
        }

        /// <inheritdoc />
        public IObjectRepository DeepClone(IObjectRepository instance, ProcessArgument processArgument, ChildChangesGetter childChangesGetter, Func<IMetadataObject, bool> mustForceVisit)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return (IObjectRepository)DeepClone((IMetadataObject)instance, processArgument, childChangesGetter, mustForceVisit);
        }

        IMetadataObject DeepClone(IMetadataObject node, ProcessArgument processArgument, ChildChangesGetter childChangesGetter, Func<IMetadataObject, bool> mustForceVisit)
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

            ILazyChildren ProcessChildren(ChildPropertyInfo childProperty, ILazyChildren children, IMetadataObject @new, IModelDataAccessor childDataAccessor)
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
