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
        readonly Lazy<ImmutableList<ChildPropertyInfo>> _childProperties;
        readonly Lazy<ImmutableList<ModifiablePropertyInfo>> _modifiableProperties;
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
        internal ModelDataAccessor(IServiceProvider serviceProvider, Type type)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dataAccessorProvider = _serviceProvider.GetService<IModelDataAccessorProvider>();
            Type = type ?? throw new ArgumentNullException(nameof(type));
            _childProperties = new Lazy<ImmutableList<ChildPropertyInfo>>(GetChildProperties);
            _modifiableProperties = new Lazy<ImmutableList<ModifiablePropertyInfo>>(GetModifiableProperties);
            _constructorBinding = new Lazy<ConstructorParameterBinding>(() =>
            {
                var constructors = from c in Type.GetConstructors()
                                   select new ConstructorParameterBinding(_serviceProvider, c);
                return serviceProvider.GetService<IConstructorSelector>().SelectConstructorBinding(Type, constructors.ToArray());
            });
        }

        /// <inheritdoc />
        public Type Type { get; }

        /// <inheritdoc />
        public ImmutableList<ChildPropertyInfo> ChildProperties => _childProperties.Value;

        /// <inheritdoc />
        public ImmutableList<ModifiablePropertyInfo> ModifiableProperties => _modifiableProperties.Value;

        /// <inheritdoc />
        public ConstructorParameterBinding ConstructorParameterBinding => _constructorBinding.Value;

        ImmutableList<ChildPropertyInfo> GetChildProperties() =>
            (from p in Type.GetProperties()
             let lazyChildrenType = LazyChildrenHelper.TryGetLazyChildrenInterface(p.PropertyType)
             where lazyChildrenType != null
             select new ChildPropertyInfo(p, lazyChildrenType.GetGenericArguments()[0])).ToImmutableList();

        ImmutableList<ModifiablePropertyInfo> GetModifiableProperties() =>
            (from p in Type.GetProperties()
             where Attribute.IsDefined(p, typeof(ModifiableAttribute))
             select new ModifiablePropertyInfo(p)).ToImmutableList();

        IMetadataObject CloneSubTree(IMetadataObject @object, Expression predicate = null)
        {
            if (@object == null)
            {
                throw new ArgumentNullException(nameof(@object));
            }

            var reflector = new PredicateReflector(predicate);
            return ConstructorParameterBinding.Cloner(
                @object,
                reflector,
                (name, children, @new, childDataAccessor) =>
                {
                    var childChanges = reflector.TryGetChildChanges(name);
                    return children.Clone(
                        n => n == @object ?
                             @new :
                             childDataAccessor.ConstructorParameterBinding.Cloner(n, PredicateReflector.Empty, RecursiveClone),
                        childChanges?.Where(c => c.Type == PredicateReflector.ChildChangeType.Add).Select(c => c.Child) ?? Enumerable.Empty<IMetadataObject>(),
                        childChanges?.Where(c => c.Type == PredicateReflector.ChildChangeType.Delete).Select(c => c.Child) ?? Enumerable.Empty<IMetadataObject>(),
                        forceVisit: false);
                });
        }

        ILazyChildren RecursiveClone(string name, ILazyChildren children, IMetadataObject @new, IModelDataAccessor childDataAccessor) =>
            children.Clone(n => childDataAccessor.ConstructorParameterBinding.Cloner(n, PredicateReflector.Empty, RecursiveClone),
                           null,
                           null,
                           forceVisit: false);

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
                    children.Clone(n => n == old ?
                                        @new :
                                        childDataAccessor.ConstructorParameterBinding.Cloner(n, new PredicateReflector(), RecursiveClone),
                                   null,
                                   null,
                                   forceVisit: false));
            @new.AttachToParent(newParent);

            CreateNewParentTree(old.Parent, newParent);
        }

        /// <inheritdoc />
        public IMetadataObject With(IMetadataObject source, Expression predicate = null)
        {
            var result = CloneSubTree(source, predicate);
            CreateNewParentTree(source, result);
            return result;
        }
    }
}
