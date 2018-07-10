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
        readonly IServiceProvider _serviceProvider;
        readonly IModelDataAccessorProvider _dataAccessorProvider;
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
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            Type = type ?? throw new ArgumentNullException(nameof(type));

            _dataAccessorProvider = _serviceProvider.GetRequiredService<IModelDataAccessorProvider>();
            _childProperties = new Lazy<IImmutableList<ChildPropertyInfo>>(GetChildProperties);
            _modifiableProperties = new Lazy<IImmutableList<ModifiablePropertyInfo>>(GetModifiableProperties);
            _constructorBinding = new Lazy<ConstructorParameterBinding>(() =>
            {
                var constructors = from c in Type.GetConstructors()
                                   select new ConstructorParameterBinding(_serviceProvider, c);
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

        IMetadataObject CloneSubTree(IMetadataObject @object, PredicateReflector reflector)
        {
            if (@object == null)
            {
                throw new ArgumentNullException(nameof(@object));
            }

            return ConstructorParameterBinding.Cloner(
                @object,
                reflector,
                (name, children, @new, childDataAccessor) =>
                {
                    var childChanges = reflector.TryGetChildChanges(name);
                    return children.Clone(
                        forceVisit: false,
                        update: n => n == @object ?
                                @new :
                                childDataAccessor.ConstructorParameterBinding.Cloner(n, PredicateReflector.Empty, RecursiveClone),
                        added: childChanges?.Where(c => c.Type == PredicateReflector.ChildChangeType.Add).Select(c => c.Child) ?? Enumerable.Empty<IMetadataObject>(),
                        deleted: childChanges?.Where(c => c.Type == PredicateReflector.ChildChangeType.Delete).Select(c => c.Child) ?? Enumerable.Empty<IMetadataObject>());
                });
        }

        ILazyChildren RecursiveClone(string name, ILazyChildren children, IMetadataObject @new, IModelDataAccessor childDataAccessor) =>
            children.Clone(forceVisit: false,
                update: n => childDataAccessor.ConstructorParameterBinding.Cloner(n, PredicateReflector.Empty, RecursiveClone),
                added: null,
                deleted: null);

        void CreateNewParentTree(IMetadataObject old, IMetadataObject @new)
        {
            if (old is AbstractInstance)
            {
                return;
            }
            if (old == null)
            {
                throw new ArgumentNullException(nameof(old));
            }
            if (@new == null)
            {
                throw new ArgumentNullException(nameof(@new));
            }
            if (old.Parent == null)
            {
                throw new NotSupportedException($"Object '{old}' does not have any parent.");
            }

            var parentDataAccessor = _dataAccessorProvider.Get(old.Parent.GetType());
            var newParent = parentDataAccessor.ConstructorParameterBinding.Cloner(
                old.Parent,
                PredicateReflector.Empty,
                (name, children, _, childDataAccessor) =>
                    children.Clone(forceVisit: false,
                        update: n => n == old ?
                                @new :
                                childDataAccessor.ConstructorParameterBinding.Cloner(n, PredicateReflector.Empty, RecursiveClone),
                        added: null,
                        deleted: null));
            @new.AttachToParent(newParent);

            CreateNewParentTree(old.Parent, newParent);
        }

        /// <inheritdoc />
        public IMetadataObject With(IMetadataObject source, Expression predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var result = CloneSubTree(source, new PredicateReflector(predicate));
            CreateNewParentTree(source, result);
            return result;
        }
    }
}
